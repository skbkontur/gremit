using System;
using System.Reflection;
using System.Reflection.Emit;

namespace GrEmit.MethodBodyParsing
{
    internal class DynamicMethodWrapper
    {
        static DynamicMethodWrapper()
        {
            var assembly = typeof(DynamicMethod).Assembly;
            dynamicScopeType = assembly.GetType("System.Reflection.Emit.DynamicScope");

            var m_DynamicILInfoField = typeof(DynamicMethod).GetField("m_DynamicILInfo", BindingFlags.Instance | BindingFlags.NonPublic);
            if (m_DynamicILInfoField == null)
                throw new InvalidOperationException("Field 'DynamicMethod.m_DynamicILInfo' is not found");
            m_DynamicILInfoExtractor = FieldsExtractor.GetExtractor<DynamicMethod, DynamicILInfo>(m_DynamicILInfoField);

            var m_ilGeneratorField = typeof(DynamicMethod).GetField("m_ilGenerator", BindingFlags.Instance | BindingFlags.NonPublic);
            if (m_ilGeneratorField == null)
                throw new InvalidOperationException("Field 'DynamicMethod.m_ilGenerator' is not found");
            m_ilGeneratorExtractor = FieldsExtractor.GetExtractor<DynamicMethod, ILGenerator>(m_ilGeneratorField);

            getDynamicIlInfo = BuildGetDynamicILInfo();
        }

        public DynamicMethodWrapper(DynamicMethod inst)
        {
            this.inst = inst;
        }

        private static Func<DynamicMethod, object, DynamicILInfo> BuildGetDynamicILInfo()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(DynamicILInfo), new[] {typeof(DynamicMethod), typeof(object)}, typeof(string), true);
            using(var il = new GroboIL(method))
            {
                il.Ldarg(0); // stack: [dynamicMethod]
                il.Ldarg(1); // stack: [dynamicMethod, scope]
                il.Castclass(dynamicScopeType);
                var getDynamicILInfoMethod = typeof(DynamicMethod).GetMethod("GetDynamicILInfo", BindingFlags.Instance | BindingFlags.NonPublic);
                if(getDynamicILInfoMethod == null)
                    throw new MissingMethodException("DynamicMethod", "GetDynamicILInfo");
                il.Call(getDynamicILInfoMethod); // stack: [dynamicMethod.GetDynamicILInfo(scope)]
                il.Ret();
            }

            return (Func<DynamicMethod, object, DynamicILInfo>)method.CreateDelegate(typeof(Func<DynamicMethod, object, DynamicILInfo>));
        }

        public DynamicILInfo m_DynamicILInfo { get { return m_DynamicILInfoExtractor(inst); } }
        public ILGenerator m_ilGenerator { get { return m_ilGeneratorExtractor(inst); } }

        public static void Init()
        {
        }

        public DynamicILInfo GetDynamicILInfoWithOldScope()
        {
            return m_DynamicILInfo ?? (m_ilGenerator == null ? inst.GetDynamicILInfo() : getDynamicIlInfo(inst, new DynamicILGenerator(m_ilGenerator).m_scope.inst));
        }

        public DynamicMethod inst;

        private static readonly Func<DynamicMethod, DynamicILInfo> m_DynamicILInfoExtractor;
        private static readonly Func<DynamicMethod, ILGenerator> m_ilGeneratorExtractor;
        private static readonly Type dynamicScopeType;
        private static readonly Func<DynamicMethod, object, DynamicILInfo> getDynamicIlInfo;
    }
}