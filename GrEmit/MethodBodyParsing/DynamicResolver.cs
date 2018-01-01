using System;
using System.Reflection;
using System.Reflection.Emit;

namespace GrEmit.MethodBodyParsing
{
    internal class DynamicResolver : IDisposable
    {
        static DynamicResolver()
        {
            var assembly = typeof(DynamicMethod).Assembly;
            dynamicResolverType = assembly.GetType("System.Reflection.Emit.DynamicResolver");
            dynamicILInfoType = assembly.GetType("System.Reflection.Emit.DynamicILInfo");
            dynamicILGeneratorType = assembly.GetType("System.Reflection.Emit.DynamicILGenerator");
            BuildFactoryByDynamicILInfo();
            BuildFactoryByDynamicILGenerator();
            BuildGetCodeInfoDelegate();
            BuildGetRawEHInfoDelegate();
            BuildGetEHInfoDelegate();

            var m_methodField = dynamicResolverType.GetField("m_method", BindingFlags.Instance | BindingFlags.NonPublic);
            if(m_methodField == null)
                throw new InvalidOperationException("Field 'DynamicResolver.m_methodField' is not found");
            m_methodSetter = FieldsExtractor.GetSetter(m_methodField);

            var m_resolverField = typeof(DynamicMethod).GetField("m_resolver", BindingFlags.Instance | BindingFlags.NonPublic);
            if(m_resolverField == null)
                throw new InvalidOperationException("Field 'DynamicResolver.m_resolver' is not found");
            m_resolverSetter = FieldsExtractor.GetSetter<DynamicMethod, object>(m_resolverField);

            var m_localSignatureField = dynamicResolverType.GetField("m_localSignature", BindingFlags.Instance | BindingFlags.NonPublic);
            if(m_localSignatureField == null)
                throw new InvalidOperationException("Field 'DynamicResolver.m_localSignature' is not found");
            m_localSignatureExtractor = FieldsExtractor.GetExtractor<object, byte[]>(m_localSignatureField);
        }

        public DynamicResolver(DynamicMethod dynamicMethod, GrEmit.Utils.DynamicILInfo dynamicILInfo)
        {
            this.dynamicMethod = dynamicMethod;
            inst = factoryByDynamicILInfo(dynamicILInfo);
        }

        public DynamicResolver(DynamicMethod dynamicMethod, ILGenerator ilGenerator)
        {
            this.dynamicMethod = dynamicMethod;
            inst = factoryByDynamicILGenerator(ilGenerator);
        }

        private static void BuildGetCodeInfoDelegate()
        {
            var parameterTypes = new[] {typeof(object), typeof(int).MakeByRefType(), typeof(int).MakeByRefType(), typeof(int).MakeByRefType()};
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(byte[]), parameterTypes, typeof(string), true);
            using(var il = new GroboIL(method))
            {
                il.Ldarg(0);
                il.Castclass(dynamicResolverType);
                il.Ldarg(1);
                il.Ldarg(2);
                il.Ldarg(3);
                var getCodeInfoMethod = dynamicResolverType.GetMethod("GetCodeInfo", BindingFlags.Instance | BindingFlags.NonPublic);
                if(getCodeInfoMethod == null)
                    throw new MissingMethodException("DynamicResolver", "GetCodeInfo");
                il.Call(getCodeInfoMethod);
                il.Ret();
            }

            getCodeInfoDelegate = (GetCodeInfoDelegate)method.CreateDelegate(typeof(GetCodeInfoDelegate));
        }

        private static void BuildGetEHInfoDelegate()
        {
            var parameterTypes = new[] {typeof(object), typeof(int), typeof(void*)};
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), parameterTypes, typeof(string), true);
            using(var il = new GroboIL(method))
            {
                il.Ldarg(0);
                il.Castclass(dynamicResolverType);
                il.Ldarg(1);
                il.Ldarg(2);
                var getEHInfoMethod = dynamicResolverType.GetMethod("GetEHInfo", BindingFlags.Instance | BindingFlags.NonPublic);
                if(getEHInfoMethod == null)
                    throw new MissingMethodException("DynamicResolver", "GetEHInfo");
                il.Call(getEHInfoMethod);
                il.Ret();
            }

            getEHInfoDelegate = (GetEHInfoDelegate)method.CreateDelegate(typeof(GetEHInfoDelegate));
        }

        private static void BuildGetRawEHInfoDelegate()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(byte[]), new[] {typeof(object)}, typeof(string), true);
            using(var il = new GroboIL(method))
            {
                il.Ldarg(0);
                il.Castclass(dynamicResolverType);
                var getRawEHInfoMethod = dynamicResolverType.GetMethod("GetRawEHInfo", BindingFlags.Instance | BindingFlags.NonPublic);
                if(getRawEHInfoMethod == null)
                    throw new MissingMethodException("DynamicResolver", "GetRawEHInfo");
                il.Call(getRawEHInfoMethod);
                il.Ret();
            }

            getRawEHInfoDelegate = (Func<object, byte[]>)method.CreateDelegate(typeof(Func<object, byte[]>));
        }

        private static void BuildFactoryByDynamicILInfo()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(object), new[] {dynamicILInfoType}, typeof(string), true);
            using(var il = new GroboIL(method))
            {
                il.Ldarg(0);
                var constructor = dynamicResolverType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] {dynamicILInfoType}, null);
                if(constructor == null)
                    throw new MissingMethodException("DynamicResolver", ".ctor");
                il.Newobj(constructor);
                il.Ret();
            }

            factoryByDynamicILInfo = (Func<GrEmit.Utils.DynamicILInfo, object>)method.CreateDelegate(typeof(Func<GrEmit.Utils.DynamicILInfo, object>));
        }

        private static void BuildFactoryByDynamicILGenerator()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(object), new[] {typeof(ILGenerator)}, typeof(string), true);
            using(var il = new GroboIL(method))
            {
                il.Ldarg(0);
                il.Castclass(dynamicILGeneratorType);
                var constructor = dynamicResolverType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] {dynamicILGeneratorType}, null);
                if(constructor == null)
                    throw new MissingMethodException("DynamicResolver", ".ctor");
                il.Newobj(constructor);
                il.Ret();
            }

            factoryByDynamicILGenerator = (Func<ILGenerator, object>)method.CreateDelegate(typeof(Func<ILGenerator, object>));
        }

        private delegate byte[] GetCodeInfoDelegate(object inst, out int stackSize, out int initLocals, out int EHCount);

        private unsafe delegate void GetEHInfoDelegate(object inst, int excNumber, void* exc);

        public void Dispose()
        {
            m_methodSetter(inst, null);
            m_resolverSetter(dynamicMethod, null);
        }

        public byte[] GetCodeInfo(out int stackSize, out int initLocals, out int EHCount)
        {
            return getCodeInfoDelegate(inst, out stackSize, out initLocals, out EHCount);
        }

        public byte[] GetRawEHInfo()
        {
            return getRawEHInfoDelegate(inst);
        }

        public unsafe void GetEHInfo(int excNumber, void* exc)
        {
            getEHInfoDelegate(inst, excNumber, exc);
        }

        public byte[] m_localSignature => m_localSignatureExtractor(inst);

        public static void Init()
        {
        }

        private readonly DynamicMethod dynamicMethod;
        private readonly object inst;

        private static Func<GrEmit.Utils.DynamicILInfo, object> factoryByDynamicILInfo;
        private static Func<ILGenerator, object> factoryByDynamicILGenerator;
        private static GetCodeInfoDelegate getCodeInfoDelegate;
        private static GetEHInfoDelegate getEHInfoDelegate;
        private static Func<object, byte[]> getRawEHInfoDelegate;
        private static readonly Action<object, object> m_methodSetter;
        private static readonly Type dynamicResolverType;
        private static readonly Type dynamicILInfoType;
        private static readonly Action<DynamicMethod, object> m_resolverSetter;
        private static readonly Func<object, byte[]> m_localSignatureExtractor;
        private static readonly Type dynamicILGeneratorType;
    }
}