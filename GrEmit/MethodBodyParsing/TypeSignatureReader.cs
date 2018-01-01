using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace GrEmit.MethodBodyParsing
{
    public class TypeSignatureReader : ByteBuffer
    {
        public TypeSignatureReader(byte[] signature, Func<MetadataToken, object> tokenResolver)
            : base(signature)
        {
            this.tokenResolver = tokenResolver;
        }

        public KeyValuePair<Type, bool> Resolve()
        {
            var elementType = (ElementType)ReadByte();
            return elementType == ElementType.Pinned
                       ? new KeyValuePair<Type, bool>(ReadTypeSignature(), true)
                       : new KeyValuePair<Type, bool>(ReadTypeSignature(elementType), false);
        }

        private Type ReadTypeSignature()
        {
            var elementType = ReadByte();

            return ReadTypeSignature((ElementType)elementType);
        }

        private Type ReadTypeTokenSignature()
        {
            var encodedToken = ReadCompressedUInt32();
            TokenType tokenType;
            switch(encodedToken & 3)
            {
            case 0:
                tokenType = TokenType.TypeDef;
                break;
            case 1:
                tokenType = TokenType.TypeRef;
                break;
            case 2:
                tokenType = TokenType.TypeSpec;
                break;
            default:
                throw new InvalidOperationException();
            }

            var token = new MetadataToken(tokenType, encodedToken >> 2);
            return (Type)tokenResolver(token);
        }

        private Type ReadTypeSignature(ElementType etype)
        {
            switch(etype)
            {
            case ElementType.ValueType:
                return ReadTypeTokenSignature();
            case ElementType.Class:
                return ReadTypeTokenSignature();
            case ElementType.Ptr:
                return ReadTypeSignature().MakePointerType();
            case ElementType.FnPtr:
            case ElementType.Pinned:
                throw new NotSupportedException();
            case ElementType.ByRef:
                return ReadTypeSignature().MakeByRefType();
            case ElementType.SzArray:
                return ReadTypeSignature().MakeArrayType();
            case ElementType.Array:
                throw new NotSupportedException();
            case ElementType.CModOpt:
                throw new NotSupportedException();
            case ElementType.CModReqD:
                throw new NotSupportedException();
            case ElementType.Sentinel:
                throw new NotSupportedException();
            case ElementType.Var:
                throw new NotSupportedException();
            case ElementType.MVar:
                throw new NotSupportedException();
            case ElementType.GenericInst:
                {
                    Type genericType;
                    var readByte = ReadByte();
                    // element_type
                    if((ElementType)readByte == ElementType.Internal)
                        genericType = ReadInternal();
                    else
                        genericType = ReadTypeTokenSignature();

                    var inst = ReadGenericInstanceSignature();
                    return genericType.MakeGenericType(inst);
                }

            case ElementType.Internal:
                return ReadInternal();
            case ElementType.Void:
                return typeof(void);
            case ElementType.Boolean:
                return typeof(bool);
            case ElementType.Char:
                return typeof(char);
            case ElementType.I1:
                return typeof(sbyte);
            case ElementType.I2:
                return typeof(short);
            case ElementType.I4:
                return typeof(int);
            case ElementType.I8:
                return typeof(long);
            case ElementType.I:
                return typeof(IntPtr);
            case ElementType.U1:
                return typeof(byte);
            case ElementType.U2:
                return typeof(ushort);
            case ElementType.U4:
                return typeof(uint);
            case ElementType.U8:
                return typeof(ulong);
            case ElementType.U:
                return typeof(UIntPtr);
            case ElementType.String:
                return typeof(string);
            case ElementType.R4:
                return typeof(float);
            case ElementType.R8:
                return typeof(double);
            default:
                throw new NotSupportedException();
            }
        }

        private Type[] ReadGenericInstanceSignature()
        {
            var arity = ReadCompressedUInt32();
            var result = new Type[arity];

            for(int i = 0; i < arity; i++)
                result[i] = ReadTypeSignature();
            return result;
        }

        private Type ReadInternal()
        {
            var typeRef = IntPtr.Size == 4
                              ? new IntPtr(ReadInt32())
                              : new IntPtr(ReadInt64());
            return Type.GetTypeFromHandle(runtimeTypeHandleCreator(typeRef));
        }

        private static Func<IntPtr, RuntimeTypeHandle> EmitRuntimeTypeHandleCreator()
        {
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(RuntimeTypeHandle), new[] {typeof(IntPtr)}, typeof(string), true);
            using(var il = new GroboIL(dynamicMethod))
            {
                var GetTypeFromHandleUnsafe_m = typeof(Type).GetMethod("GetTypeFromHandleUnsafe", BindingFlags.Static | BindingFlags.NonPublic);
                il.Ldarg(0); // stack: [ptr]
                il.Call(GetTypeFromHandleUnsafe_m); // stack: [Type.GetTypeFromHandleUnsafe(ptr)]
                var runtimeType = typeof(Type).Assembly.GetType("System.RuntimeType");
                var constructor = typeof(RuntimeTypeHandle).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] {runtimeType}, null);
                il.Newobj(constructor);
                il.Ret();
            }
            return (Func<IntPtr, RuntimeTypeHandle>)dynamicMethod.CreateDelegate(typeof(Func<IntPtr, RuntimeTypeHandle>));
        }

        private readonly Func<MetadataToken, object> tokenResolver;

        private static readonly Func<IntPtr, RuntimeTypeHandle> runtimeTypeHandleCreator = EmitRuntimeTypeHandleCreator();
    }
}