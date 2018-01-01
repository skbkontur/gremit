using System;
using System.Reflection;
using System.Reflection.Emit;

using GrEmit.Utils;

namespace GrEmit.MethodBodyParsing
{
    internal static class DynamicMethodHelpers
    {
        public static object Resolve(object value)
        {
            return ResolveRuntimeMethodHandle(value)
                   ?? ResolveRuntimeFieldHandle(value)
                   ?? ResolveRuntimeTypeHandle(value)
                   ?? resolveGenericMethodInfo(value)
                   ?? resolveVarArgsMethod(value)
                   ?? resolveGenericFieldInfo(value)
                   ?? value;
        }

        private static object ResolveRuntimeMethodHandle(object value)
        {
            if(value is RuntimeMethodHandle)
                return MethodBase.GetMethodFromHandle((RuntimeMethodHandle)value);
            return null;
        }

        private static object ResolveRuntimeFieldHandle(object value)
        {
            if(value is RuntimeFieldHandle)
                return FieldInfo.GetFieldFromHandle((RuntimeFieldHandle)value);
            return null;
        }

        private static object ResolveRuntimeTypeHandle(object value)
        {
            if(value is RuntimeTypeHandle)
                return Type.GetTypeFromHandle((RuntimeTypeHandle)value);
            return null;
        }

        private static Func<object, object> EmitResolveGenericMethodInfo()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(object), new[] {typeof(object)}, typeof(string), true);
            using(var il = new GroboIL(method))
            {
                var GenericMethodInfo_t = typeof(DynamicMethod).Assembly.GetType("System.Reflection.Emit.GenericMethodInfo");
                if(GenericMethodInfo_t == null)
                    throw new InvalidOperationException("Missing type 'System.Reflection.Emit.GenericMethodInfo'");
                il.Ldarg(0); // stack: [value]
                il.Isinst(GenericMethodInfo_t); // stack: [(GenericMethodInfo)value]
                var retLabel = il.DefineLabel("ret");
                il.Dup(); // stack: [(GenericMethodInfo)value, (GenericMethodInfo)value]
                il.Brfalse(retLabel); // if(!(value is GenericMethodInfo)) goto ret; stack: [value as GenericMethodInfo]
                var m_methodHandle_f = GenericMethodInfo_t.GetField("m_methodHandle", BindingFlags.Instance | BindingFlags.NonPublic);
                if(m_methodHandle_f == null)
                    throw new InvalidOperationException("Missing field 'System.Reflection.Emit.GenericMethodInfo.m_methodHandle'");
                var m_context_f = GenericMethodInfo_t.GetField("m_context", BindingFlags.Instance | BindingFlags.NonPublic);
                if(m_context_f == null)
                    throw new InvalidOperationException("Missing field 'System.Reflection.Emit.GenericMethodInfo.m_context'");
                var temp = il.DeclareLocal(GenericMethodInfo_t);
                il.Dup();
                il.Stloc(temp); // temp = (GenericMethodInfo)value; stack: [(GenericMethodInfo)value]
                il.Ldfld(m_methodHandle_f); // stack: [((GenericMethodInfo)value).m_methodHandle]
                il.Ldloc(temp); // stack: [((GenericMethodInfo)value).m_methodHandle, (GenericMethodInfo)value]
                il.Ldfld(m_context_f); // stack: [((GenericMethodInfo)value).m_methodHandle, ((GenericMethodInfo)value).m_context]
                var getMethodFromHandle_m = HackHelpers.GetMethodDefinition<int>(x => MethodBase.GetMethodFromHandle(default(RuntimeMethodHandle), default(RuntimeTypeHandle)));
                il.Call(getMethodFromHandle_m); // stack: [MethodBase.GetMethodFromHandle(((GenericMethodInfo)value).m_methodHandle, ((GenericMethodInfo)value).m_context)]
                il.MarkLabel(retLabel);
                il.Ret();
            }
            return (Func<object, object>)method.CreateDelegate(typeof(Func<object, object>));
        }

        private static Func<object, object> EmitResolveVarArgsMethod()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(object), new[] {typeof(object)}, typeof(string), true);
            using(var il = new GroboIL(method))
            {
                var VarArgMethod_t = typeof(DynamicMethod).Assembly.GetType("System.Reflection.Emit.VarArgMethod");
                if(VarArgMethod_t == null)
                    throw new InvalidOperationException("Missing type 'System.Reflection.Emit.VarArgMethod'");
                il.Ldarg(0); // stack: [value]
                il.Isinst(VarArgMethod_t); // stack: [(VarArgMethod)value]
                var retLabel = il.DefineLabel("ret");
                il.Dup(); // stack: [(VarArgMethod)value, (VarArgMethod)value]
                il.Brfalse(retLabel); // if(!(value is VarArgMethod)) goto ret; stack: [value as VarArgMethod]
                var m_method_f = VarArgMethod_t.GetField("m_method", BindingFlags.Instance | BindingFlags.NonPublic);
                if(m_method_f == null)
                    throw new InvalidOperationException("Missing field 'System.Reflection.Emit.VarArgMethod.m_method'");
                var m_dynamicMethod_f = VarArgMethod_t.GetField("m_dynamicMethod", BindingFlags.Instance | BindingFlags.NonPublic);
                if(m_dynamicMethod_f == null)
                    throw new InvalidOperationException("Missing field 'System.Reflection.Emit.VarArgMethod.m_dynamicMethod'");
                var temp = il.DeclareLocal(VarArgMethod_t);
                il.Dup();
                il.Stloc(temp); // temp = (VarArgMethod)value; stack: [(VarArgMethod)value]
                il.Ldfld(m_method_f); // stack: [((VarArgMethod)value).m_method]
                il.Dup(); // stack: [((VarArgMethod)value).m_method, ((VarArgMethod)value).m_method]
                il.Brtrue(retLabel); // if(((VarArgMethod)value).m_method != null) goto ret; stack: [((VarArgMethod)value).m_method]
                il.Pop(); // stack: []
                il.Ldloc(temp); // stack: [(VarArgMethod)value]
                il.Ldfld(m_dynamicMethod_f); // stack: [((VarArgMethod)value).m_dynamicMethod]
                il.MarkLabel(retLabel);
                il.Ret();
            }
            return (Func<object, object>)method.CreateDelegate(typeof(Func<object, object>));
        }

        private static Func<object, object> EmitResolveGenericFieldInfo()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(object), new[] {typeof(object)}, typeof(string), true);
            using(var il = new GroboIL(method))
            {
                var GenericFieldInfo_t = typeof(DynamicMethod).Assembly.GetType("System.Reflection.Emit.GenericFieldInfo");
                if(GenericFieldInfo_t == null)
                    throw new InvalidOperationException("Missing type 'System.Reflection.Emit.GenericFieldInfo'");
                il.Ldarg(0); // stack: [value]
                il.Isinst(GenericFieldInfo_t); // stack: [(GenericFieldInfo)value]
                var retLabel = il.DefineLabel("ret");
                il.Dup(); // stack: [(GenericFieldInfo)value, (GenericFieldInfo)value]
                il.Brfalse(retLabel); // if(!(value is GenericFieldInfo)) goto ret; stack: [value as GenericFieldInfo]
                var m_fieldHandle_f = GenericFieldInfo_t.GetField("m_fieldHandle", BindingFlags.Instance | BindingFlags.NonPublic);
                if(m_fieldHandle_f == null)
                    throw new InvalidOperationException("Missing field 'System.Reflection.Emit.GenericFieldInfo.m_fieldHandle'");
                var m_context_f = GenericFieldInfo_t.GetField("m_context", BindingFlags.Instance | BindingFlags.NonPublic);
                if(m_context_f == null)
                    throw new InvalidOperationException("Missing field 'System.Reflection.Emit.GenericFieldInfo.m_context'");
                var temp = il.DeclareLocal(GenericFieldInfo_t);
                il.Dup();
                il.Stloc(temp); // temp = (GenericFieldInfo)value; stack: [(GenericFieldInfo)value]
                il.Ldfld(m_fieldHandle_f); // stack: [((GenericFieldInfo)value).m_fieldHandle]
                il.Ldloc(temp); // stack: [((GenericFieldInfo)value).m_fieldHandle, (GenericFieldInfo)value]
                il.Ldfld(m_context_f); // stack: [((GenericFieldInfo)value).m_fieldHandle, ((GenericFieldInfo)value).m_context]
                var getFieldFromHandle_m = HackHelpers.GetMethodDefinition<int>(x => FieldInfo.GetFieldFromHandle(default(RuntimeFieldHandle), default(RuntimeTypeHandle)));
                il.Call(getFieldFromHandle_m); // stack: [MethodBase.GetMethodFromHandle(((GenericFieldInfo)value).m_fieldHandle, ((GenericFieldInfo)value).m_context)]
                il.MarkLabel(retLabel);
                il.Ret();
            }
            return (Func<object, object>)method.CreateDelegate(typeof(Func<object, object>));
        }

        private static readonly Func<object, object> resolveGenericMethodInfo = EmitResolveGenericMethodInfo();

        private static readonly Func<object, object> resolveVarArgsMethod = EmitResolveVarArgsMethod();

        private static readonly Func<object, object> resolveGenericFieldInfo = EmitResolveGenericFieldInfo();
    }

    internal class MethodBodyOnDynamicILGenerator : MethodBody
    {
        public MethodBodyOnDynamicILGenerator(DynamicMethod dynamicMethod, ILGenerator ilGenerator, bool resolveTokens)
            : base(GetMethodSignature(ilGenerator), resolveTokens)
        {
            scope = new DynamicILGenerator(ilGenerator).m_scope;
            using(var dynamicResolver = new DynamicResolver(dynamicMethod, ilGenerator))
            {
                int stackSize;
                int initLocals;
                int EHCount;

                var code = dynamicResolver.GetCodeInfo(out stackSize, out initLocals, out EHCount);

                MaxStack = stackSize;
                InitLocals = initLocals != 0;

                SetLocalSignature(dynamicResolver.m_localSignature);

                ILCodeReader.Read(code, ResolveToken, resolveTokens, this);

                ReadExceptions(dynamicResolver, EHCount);
            }
        }

        protected override object ResolveToken(MetadataToken token)
        {
            return DynamicMethodHelpers.Resolve(scope[token.ToInt32()]);
        }

        private struct CORINFO_EH_CLAUSE
        {
            internal int Flags;
            internal int TryOffset;
            internal int TryLength;
            internal int HandlerOffset;
            internal int HandlerLength;
            internal int ClassTokenOrFilterOffset;
        }

        private unsafe void ReadExceptions(DynamicResolver dynamicResolver, int excCount)
        {
            var buf = stackalloc CORINFO_EH_CLAUSE[1];
            var exceptionClause = &buf[0];

            for(int i = 0; i < excCount; ++i)
            {
                dynamicResolver.GetEHInfo(i, exceptionClause);

                var handler = new ExceptionHandler((ExceptionHandlerType)exceptionClause->Flags);

                handler.TryStart = GetInstruction(exceptionClause->TryOffset);
                handler.TryEnd = GetInstruction(handler.TryStart.Offset + exceptionClause->TryLength);

                handler.HandlerStart = GetInstruction(exceptionClause->HandlerOffset);
                handler.HandlerEnd = GetInstruction(handler.HandlerStart.Offset + exceptionClause->HandlerLength);

                switch(handler.HandlerType)
                {
                case ExceptionHandlerType.Catch:
                    var token = new MetadataToken((uint)exceptionClause->ClassTokenOrFilterOffset);
                    handler.CatchType = resolveTokens ? ResolveToken(token) : token;
                    break;
                case ExceptionHandlerType.Filter:
                    handler.FilterStart = GetInstruction(exceptionClause->ClassTokenOrFilterOffset);
                    break;
                }

                ExceptionHandlers.Add(handler);
            }
        }

        private static byte[] GetMethodSignature(ILGenerator ilGenerator)
        {
            var wrapper = new DynamicILGenerator(ilGenerator);
            return (byte[])wrapper.m_scope[wrapper.m_methodSigToken];
        }

        private readonly DynamicScope scope;
    }
}