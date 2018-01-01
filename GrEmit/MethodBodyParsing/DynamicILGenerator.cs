using System;
using System.Reflection;
using System.Reflection.Emit;

namespace GrEmit.MethodBodyParsing
{
    internal class DynamicILGenerator
    {
        static DynamicILGenerator()
        {
            var assembly = typeof(DynamicMethod).Assembly;
            var dynamicILGeneratorType = assembly.GetType("System.Reflection.Emit.DynamicILGenerator");

            var m_methodSigTokenField = dynamicILGeneratorType.GetField("m_methodSigToken", BindingFlags.Instance | BindingFlags.NonPublic);
            if(m_methodSigTokenField == null)
                throw new InvalidOperationException("Field 'DynamicILGenerator.m_methodSigToken' is not found");
            m_methodSigTokenExtractor = FieldsExtractor.GetExtractor<ILGenerator, int>(m_methodSigTokenField);

            var m_scopeField = dynamicILGeneratorType.GetField("m_scope", BindingFlags.Instance | BindingFlags.NonPublic);
            if(m_scopeField == null)
                throw new InvalidOperationException("Field 'DynamicILGenerator.m_scope' is not found");
            m_scopeExtractor = FieldsExtractor.GetExtractor<ILGenerator, object>(m_scopeField);
        }

        public DynamicILGenerator(ILGenerator inst)
        {
            this.inst = inst;
        }

        public int m_methodSigToken => m_methodSigTokenExtractor(inst);
        public DynamicScope m_scope => new DynamicScope(m_scopeExtractor(inst));

        public static void Init()
        {
        }

        public ILGenerator inst;

        private static readonly Func<ILGenerator, int> m_methodSigTokenExtractor;
        private static readonly Func<ILGenerator, object> m_scopeExtractor;
    }
}