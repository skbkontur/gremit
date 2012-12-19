using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using GrEmit.InstructionComments;
using GrEmit.InstructionParameters;

namespace GrEmit
{
    public class GroboIL
    {
        public GroboIL(DynamicMethod method, bool analyzeStack = true)
        {
            this.analyzeStack = analyzeStack;
            il = method.GetILGenerator();
            methodReturnType = method.ReturnType;
            methodParameterTypes = Formatter.GetParameterTypes(method);
        }

        public GroboIL(MethodBuilder method, bool analyzeStack = true)
        {
            this.analyzeStack = analyzeStack;
            il = method.GetILGenerator();
            methodReturnType = method.ReturnType;
            Type[] parameterTypes = Formatter.GetParameterTypes(method);
            methodParameterTypes = method.IsStatic ? parameterTypes : new[] {method.ReflectedType}.Concat(parameterTypes).ToArray();
        }

        public GroboIL(ConstructorBuilder constructor, bool analyzeStack = true)
        {
            this.analyzeStack = analyzeStack;
            il = constructor.GetILGenerator();
            methodReturnType = typeof(void);
            methodParameterTypes = new[] {constructor.ReflectedType}.Concat(Formatter.GetParameterTypes(constructor)).ToArray();
        }

        public string GetILCode()
        {
            return ilCode.ToString();
        }

        public Local DeclareLocal(Type localType, string name, bool pinned = false)
        {
            return new Local(il.DeclareLocal(localType, pinned), (string.IsNullOrEmpty(name) ? "local" : name) + "_" + locals++);
        }

        public Local DeclareLocal(Type localType, bool pinned = false)
        {
            return new Local(il.DeclareLocal(localType, pinned), "local_" + locals++);
        }

        public Label DefineLabel(string name)
        {
            return new Label(il.DefineLabel(), name + "_" + labels++);
        }

        public void MarkLabel(Label label)
        {
            if(analyzeStack)
                MutateStack(default(OpCode), new LabelILInstructionParameter(label));
            ilCode.MarkLabel(label, GetComment());
            il.MarkLabel(label);
        }

        public void Ret()
        {
            Emit(OpCodes.Ret);
            stack = null;
        }

        public void Br(Label label)
        {
            if(label == null)
                throw new ArgumentNullException("label");
            Emit(OpCodes.Br, label);
            stack = null;
        }

        public void Brfalse(Label label)
        {
            if(label == null)
                throw new ArgumentNullException("label");
            Emit(OpCodes.Brfalse, label);
        }

        public void Brtrue(Label label)
        {
            if(label == null)
                throw new ArgumentNullException("label");
            Emit(OpCodes.Brtrue, label);
        }

        public void Ble(Type type, Label label)
        {
            if(label == null)
                throw new ArgumentNullException("label");
            Emit(Unsigned(type) ? OpCodes.Ble_Un : OpCodes.Ble, label);
        }

        public void Bge(Type type, Label label)
        {
            if(label == null)
                throw new ArgumentNullException("label");
            Emit(Unsigned(type) ? OpCodes.Bge_Un : OpCodes.Bge, label);
        }

        public void Blt(Type type, Label label)
        {
            if(label == null)
                throw new ArgumentNullException("label");
            Emit(Unsigned(type) ? OpCodes.Blt_Un : OpCodes.Blt, label);
        }

        public void Bgt(Type type, Label label)
        {
            if(label == null)
                throw new ArgumentNullException("label");
            Emit(Unsigned(type) ? OpCodes.Bgt_Un : OpCodes.Bgt, label);
        }

        public void Bne(Label label)
        {
            if(label == null)
                throw new ArgumentNullException("label");
            Emit(OpCodes.Bne_Un, label);
        }

        public void Pop()
        {
            Emit(OpCodes.Pop);
        }

        public void Dup()
        {
            Emit(OpCodes.Dup);
        }

        public void Ldloca(Local local)
        {
            Emit(OpCodes.Ldloca, local);
        }

        public void Ldloc(Local local)
        {
            Emit(OpCodes.Ldloc, local);
        }

        public void Stloc(Local local)
        {
            Emit(OpCodes.Stloc, local);
        }

        public void Ldnull()
        {
            Emit(OpCodes.Ldnull);
        }

        public void Initobj(Type type)
        {
            if(type == null)
                throw new ArgumentNullException("type");
            if(!type.IsValueType)
                throw new ArgumentException("A value type expected", "type");
            Emit(OpCodes.Initobj, type);
        }

        public void Ldarg(int index)
        {
            switch(index)
            {
            case 0:
                Emit(OpCodes.Ldarg_0);
                break;
            case 1:
                Emit(OpCodes.Ldarg_1);
                break;
            case 2:
                Emit(OpCodes.Ldarg_2);
                break;
            case 3:
                Emit(OpCodes.Ldarg_3);
                break;
            default:
                if(index < 256)
                    Emit(OpCodes.Ldarg_S, (byte)index);
                else
                    Emit(OpCodes.Ldarg, index);
                break;
            }
        }

        public void Ldarga(int index)
        {
            if(index < 256)
                Emit(OpCodes.Ldarga_S, (byte)index);
            else
                Emit(OpCodes.Ldarga, index);
        }

        public void Ldc_I4(int value)
        {
            switch(value)
            {
            case 0:
                Emit(OpCodes.Ldc_I4_0);
                break;
            case 1:
                Emit(OpCodes.Ldc_I4_1);
                break;
            case 2:
                Emit(OpCodes.Ldc_I4_2);
                break;
            case 3:
                Emit(OpCodes.Ldc_I4_3);
                break;
            case 4:
                Emit(OpCodes.Ldc_I4_4);
                break;
            case 5:
                Emit(OpCodes.Ldc_I4_5);
                break;
            case 6:
                Emit(OpCodes.Ldc_I4_6);
                break;
            case 7:
                Emit(OpCodes.Ldc_I4_7);
                break;
            case 8:
                Emit(OpCodes.Ldc_I4_8);
                break;
            case -1:
                Emit(OpCodes.Ldc_I4_M1);
                break;
            default:
                if(value < 128 && value >= -128)
                    Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                else
                    Emit(OpCodes.Ldc_I4, value);
                break;
            }
        }

        public void Ldc_I8(long value)
        {
            Emit(OpCodes.Ldc_I8, value);
        }

        public void Ldc_R4(float value)
        {
            Emit(OpCodes.Ldc_R4, value);
        }

        public void Ldc_R8(double value)
        {
            Emit(OpCodes.Ldc_R8, value);
        }

        public void Ldc_IntPtr(IntPtr value)
        {
            if(IntPtr.Size == 4)
                Ldc_I4(value.ToInt32());
            else
                Ldc_I8(value.ToInt64());
        }

        public void Ldlen()
        {
            Emit(OpCodes.Ldlen);
        }

        public void Ldftn(MethodInfo method)
        {
            if(method == null)
                throw new ArgumentNullException("method");
            Emit(OpCodes.Ldftn, method);
        }

        public void Stfld(FieldInfo field)
        {
            if (field == null)
                throw new ArgumentNullException("field");
            Emit(OpCodes.Stfld, field);
        }

        public void Ldfld(FieldInfo field)
        {
            if(field == null)
                throw new ArgumentNullException("field");
            Emit(OpCodes.Ldfld, field);
        }

        public void Ldflda(FieldInfo field)
        {
            if(field == null)
                throw new ArgumentNullException("field");
            Emit(OpCodes.Ldflda, field);
        }

        public void Ldelema(Type elementType)
        {
            if(elementType == null)
                throw new ArgumentNullException("elementType");
            Emit(OpCodes.Ldelema, elementType);
        }

        public void Ldelem(Type elementType)
        {
            if(elementType == null)
                throw new ArgumentNullException("elementType");
            if(IsStruct(elementType))
            {
                // struct
                Ldelema(elementType);
                Ldobj(elementType);
                return;
            }
            var parameter = new TypeILInstructionParameter(elementType);
            if(!elementType.IsValueType) // class
                Emit(OpCodes.Ldelem_Ref, parameter);
            else
            {
                // Primitive
                switch(Type.GetTypeCode(elementType))
                {
                case TypeCode.Boolean:
                case TypeCode.SByte:
                    Emit(OpCodes.Ldelem_I1, parameter);
                    break;
                case TypeCode.Byte:
                    Emit(OpCodes.Ldelem_U1, parameter);
                    break;
                case TypeCode.Int16:
                    Emit(OpCodes.Ldelem_I2, parameter);
                    break;
                case TypeCode.Int32:
                    Emit(OpCodes.Ldelem_I4, parameter);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    Emit(OpCodes.Ldelem_I8, parameter);
                    break;
                case TypeCode.Char:
                case TypeCode.UInt16:
                    Emit(OpCodes.Ldelem_U2, parameter);
                    break;
                case TypeCode.UInt32:
                    Emit(OpCodes.Ldelem_U4, parameter);
                    break;
                case TypeCode.Single:
                    Emit(OpCodes.Ldelem_R4, parameter);
                    break;
                case TypeCode.Double:
                    Emit(OpCodes.Ldelem_R8, parameter);
                    break;
                default:
                    throw new NotSupportedException("Type '" + elementType.Name + "' is not supported");
                }
            }
        }

        public void Stelem(Type elementType)
        {
            if(elementType == null)
                throw new ArgumentNullException("elementType");
            if(IsStruct(elementType))
                throw new InvalidOperationException("To store an item to an array of structs use Ldelema & Stobj instructions");

            var parameter = new TypeILInstructionParameter(elementType);
            if(!elementType.IsValueType) // class
                Emit(OpCodes.Stelem_Ref, parameter);
            else
            {
                // Primitive
                switch(Type.GetTypeCode(elementType))
                {
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                    Emit(OpCodes.Stelem_I1, parameter);
                    break;
                case TypeCode.Char:
                case TypeCode.UInt16:
                case TypeCode.Int16:
                    Emit(OpCodes.Stelem_I2, parameter);
                    break;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    Emit(OpCodes.Stelem_I4, parameter);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    Emit(OpCodes.Stelem_I8, parameter);
                    break;
                case TypeCode.Single:
                    Emit(OpCodes.Stelem_R4, parameter);
                    break;
                case TypeCode.Double:
                    Emit(OpCodes.Stelem_R8, parameter);
                    break;
                default:
                    throw new NotSupportedException("Type '" + elementType.Name + "' is not supported");
                }
            }
        }

        public void Stind(Type type)
        {
            if(type == null)
                throw new ArgumentNullException("type");
            if(IsStruct(type))
            {
                Stobj(type);
                return;
            }

            var parameter = new TypeILInstructionParameter(type);
            if(!type.IsValueType) // class
                Emit(OpCodes.Stind_Ref, parameter);
            else
            {
                // Primitive
                switch(Type.GetTypeCode(type))
                {
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                    Emit(OpCodes.Stind_I1, parameter);
                    break;
                case TypeCode.Int16:
                case TypeCode.Char:
                case TypeCode.UInt16:
                    Emit(OpCodes.Stind_I2, parameter);
                    break;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    Emit(OpCodes.Stind_I4, parameter);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    Emit(OpCodes.Stind_I8, parameter);
                    break;
                case TypeCode.Single:
                    Emit(OpCodes.Stind_R4, parameter);
                    break;
                case TypeCode.Double:
                    Emit(OpCodes.Stind_R8, parameter);
                    break;
                default:
                    throw new NotSupportedException("Type '" + type.Name + "' is not supported");
                }
            }
        }

        public void Ldind(Type type)
        {
            if(type == null)
                throw new ArgumentNullException("type");
            if(IsStruct(type))
            {
                Ldobj(type);
                return;
            }

            var parameter = new TypeILInstructionParameter(type);
            if(!type.IsValueType) // class
                Emit(OpCodes.Ldind_Ref, parameter);
            else
            {
                switch(Type.GetTypeCode(type))
                {
                case TypeCode.SByte:
                    Emit(OpCodes.Ldind_I1, parameter);
                    break;
                case TypeCode.Byte:
                case TypeCode.Boolean:
                    Emit(OpCodes.Ldind_U1, parameter);
                    break;
                case TypeCode.Int16:
                    Emit(OpCodes.Ldind_I2, parameter);
                    break;
                case TypeCode.Char:
                case TypeCode.UInt16:
                    Emit(OpCodes.Ldind_U2, parameter);
                    break;
                case TypeCode.Int32:
                    Emit(OpCodes.Ldind_I4, parameter);
                    break;
                case TypeCode.UInt32:
                    Emit(OpCodes.Ldind_U4, parameter);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    Emit(OpCodes.Ldind_I8, parameter);
                    break;
                case TypeCode.Single:
                    Emit(OpCodes.Ldind_R4, parameter);
                    break;
                case TypeCode.Double:
                    Emit(OpCodes.Ldind_R8, parameter);
                    break;
                default:
                    throw new NotSupportedException("Type '" + type.Name + "' is not supported");
                }
            }
        }

        public void Castclass(Type type)
        {
            if(type == null)
                throw new ArgumentNullException("type");
            if(type.IsValueType)
                throw new ArgumentException("A reference type expected", "type");
            Emit(OpCodes.Castclass, type);
        }

        public void Unbox_Any(Type type)
        {
            if(type == null)
                throw new ArgumentNullException("type");
            if(!type.IsValueType)
                throw new ArgumentException("A value type expected", "type");
            Emit(OpCodes.Unbox_Any, type);
        }

        public void Box(Type type)
        {
            if(type == null)
                throw new ArgumentNullException("type");
            if(!type.IsValueType)
                throw new ArgumentException("A value type expected", "type");
            Emit(OpCodes.Box, type);
        }

        public void WriteLine(Local local)
        {
            il.EmitWriteLine(local);
        }

        public void WriteLine(string str)
        {
            il.EmitWriteLine(str);
        }

        public void Stobj(Type type)
        {
            if(type == null)
                throw new ArgumentNullException("type");
            if(!type.IsValueType)
                throw new ArgumentException("A value type expected", "type");
            Emit(OpCodes.Stobj, type);
        }

        public void Ldobj(Type type)
        {
            if(type == null)
                throw new ArgumentNullException("type");
            if(!type.IsValueType)
                throw new ArgumentException("A value type expected", "type");
            Emit(OpCodes.Ldobj, type);
        }

        public void Newobj(ConstructorInfo constructor)
        {
            if(constructor == null)
                throw new ArgumentNullException("constructor");
            Emit(OpCodes.Newobj, constructor);
        }

        public void Newarr(Type type)
        {
            if(type == null)
                throw new ArgumentNullException("type");
            Emit(OpCodes.Newarr, type);
        }

        public void Ceq()
        {
            Emit(OpCodes.Ceq);
        }

        public void Cgt(Type type)
        {
            Emit(Unsigned(type) ? OpCodes.Cgt_Un : OpCodes.Cgt);
        }

        public void Clt(Type type)
        {
            Emit(Unsigned(type) ? OpCodes.Clt_Un : OpCodes.Clt);
        }

        public void And()
        {
            Emit(OpCodes.And);
        }

        public void Or()
        {
            Emit(OpCodes.Or);
        }

        public void Xor()
        {
            Emit(OpCodes.Xor);
        }

        public void Add()
        {
            Emit(OpCodes.Add);
        }

        public void Sub()
        {
            Emit(OpCodes.Sub);
        }

        public void Mul()
        {
            Emit(OpCodes.Mul);
        }

        public void Div(Type type)
        {
            Emit(Unsigned(type) ? OpCodes.Div_Un : OpCodes.Div);
        }

        public void Rem(Type type)
        {
            Emit(Unsigned(type) ? OpCodes.Rem_Un : OpCodes.Rem);
        }

        public void Ldstr(string value)
        {
            Emit(OpCodes.Ldstr, value);
        }

        public void Call(MethodInfo method, Type type = null, Type[] optionalParameterTypes = null)
        {
            OpCode opCode = method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call;
            if(opCode == OpCodes.Callvirt)
            {
                if(type == null)
                    throw new ArgumentNullException("type", "Type must be specified for a virtual method call");
                if(type.IsValueType)
                    Emit(OpCodes.Constrained, type);
            }
            var parameter = new MethodILInstructionParameter(method);
            if(analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.Append(opCode, parameter, GetComment());
            il.EmitCall(opCode, method, optionalParameterTypes);
        }

        public void Calli(CallingConventions callingConvention, Type returnType, Type[] parameterTypes, Type[] optionalParameterTypes = null)
        {
            var parameter = new MethodByAddressILInstructionParameter(returnType, parameterTypes);
            if(analyzeStack && stack != null)
                MutateStack(OpCodes.Calli, parameter);
            ilCode.Append(OpCodes.Calli, parameter, GetComment());
            il.EmitCalli(OpCodes.Calli, callingConvention, returnType, parameterTypes, optionalParameterTypes);
        }

        public class Label
        {
            public Label(System.Reflection.Emit.Label label, string name)
            {
                this.label = label;
                this.name = name;
            }

            public static implicit operator System.Reflection.Emit.Label(Label label)
            {
                return label.label;
            }

            public string Name { get { return name; } }

            private readonly System.Reflection.Emit.Label label;
            private readonly string name;
        }

        public class Local
        {
            public Local(LocalBuilder localBuilder, string name)
            {
                this.localBuilder = localBuilder;
                this.name = name;
            }

            public static implicit operator LocalBuilder(Local local)
            {
                return local.localBuilder;
            }

            public string Name { get { return name; } }
            public Type Type { get { return localBuilder.LocalType; } }

            private readonly LocalBuilder localBuilder;
            private readonly string name;
        }

        internal readonly Dictionary<Label, Type[]> labelStacks = new Dictionary<Label, Type[]>();
        internal readonly ILCode ilCode = new ILCode();
        internal readonly Type methodReturnType;
        internal readonly Type[] methodParameterTypes;

        private void MutateStack(OpCode opCode, ILInstructionParameter parameter)
        {
            StackMutatorCollection.Mutate(opCode, this, parameter, ref stack);
        }

        private static bool IsStruct(Type type)
        {
            return type.IsValueType && !type.IsPrimitive && !type.IsEnum;
        }

        private static bool Unsigned(Type type)
        {
            switch(Type.GetTypeCode(type))
            {
            case TypeCode.Boolean:
            case TypeCode.Byte:
            case TypeCode.Char:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
                return true;
            case TypeCode.SByte:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Single:
            case TypeCode.Double:
                return false;
            default:
                throw new NotSupportedException("Type '" + type.Name + "' is not supported");
            }
        }

        private ILInstructionComment GetComment()
        {
            return stack == null ? (ILInstructionComment)new InaccessibleCodeILInstructionComment() : new StackILInstructionComment(stack.Reverse().ToArray());
        }

        private void Emit(OpCode opCode, ILInstructionParameter parameter)
        {
            var lineNumber = ilCode.Append(opCode, new EmptyILInstructionComment());
            if (analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            il.Emit(opCode);
        }

        private void Emit(OpCode opCode)
        {
            var lineNumber = ilCode.Append(opCode, new EmptyILInstructionComment());
            if (analyzeStack && stack != null)
                MutateStack(opCode, null);
            ilCode.SetComment(lineNumber, GetComment());
            il.Emit(opCode);
        }

        private void Emit(OpCode opCode, Local local)
        {
            var parameter = new LocalILInstructionParameter(local);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if (analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            il.Emit(opCode, local);
        }

        private void Emit(OpCode opCode, Type type)
        {
            var parameter = new TypeILInstructionParameter(type);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if (analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            il.Emit(opCode, type);
        }

        private void Emit(OpCode opCode, byte value)
        {
            var parameter = new PrimitiveILInstructionParameter(value);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if (analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            il.Emit(opCode, value);
        }

        private void Emit(OpCode opCode, int value)
        {
            var parameter = new PrimitiveILInstructionParameter(value);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if (analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            il.Emit(opCode, value);
        }

        private void Emit(OpCode opCode, sbyte value)
        {
            var parameter = new PrimitiveILInstructionParameter(value);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if (analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            il.Emit(opCode, value);
        }

        private void Emit(OpCode opCode, long value)
        {
            var parameter = new PrimitiveILInstructionParameter(value);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if (analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            il.Emit(opCode, value);
        }

        private void Emit(OpCode opCode, double value)
        {
            var parameter = new PrimitiveILInstructionParameter(value);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if (analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            il.Emit(opCode, value);
        }

        private void Emit(OpCode opCode, float value)
        {
            var parameter = new PrimitiveILInstructionParameter(value);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if (analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            il.Emit(opCode, value);
        }

        private void Emit(OpCode opCode, string value)
        {
            var parameter = new StringILInstructionParameter(value);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if (analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            il.Emit(opCode, value);
        }

        private void Emit(OpCode opCode, Label label)
        {
            var parameter = new LabelILInstructionParameter(label);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if (analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            il.Emit(opCode, label);
        }

        private void Emit(OpCode opCode, FieldInfo field)
        {
            var parameter = new FieldILInstructionParameter(field);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if (analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            il.Emit(opCode, field);
        }

        private void Emit(OpCode opCode, MethodInfo method)
        {
            var parameter = new MethodILInstructionParameter(method);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if (analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            il.Emit(opCode, method);
        }

        private void Emit(OpCode opCode, ConstructorInfo constructor)
        {
            var parameter = new ConstructorILInstructionParameter(constructor);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if (analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            il.Emit(opCode, constructor);
        }

        private int locals;
        private int labels;

        private Stack<Type> stack = new Stack<Type>();

        private readonly ILGenerator il;
        private readonly bool analyzeStack = true;
    }
}