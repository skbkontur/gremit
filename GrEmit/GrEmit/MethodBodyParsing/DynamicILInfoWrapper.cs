using System;
using System.Reflection;
using System.Reflection.Emit;

namespace GrEmit.MethodBodyParsing
{
    internal class DynamicILInfoWrapper
    {
        static DynamicILInfoWrapper()
        {
            var m_methodSignatureField = typeof(DynamicILInfo).GetField("m_methodSignature", BindingFlags.Instance | BindingFlags.NonPublic);
            if (m_methodSignatureField == null)
                throw new InvalidOperationException("Field 'DynamicILInfo.m_methodSignature' is not found");
            m_methodSignatureExtractor = FieldsExtractor.GetExtractor<DynamicILInfo, int>(m_methodSignatureField);

            var m_scopeField = typeof(DynamicILInfo).GetField("m_scope", BindingFlags.Instance | BindingFlags.NonPublic);
            if (m_scopeField == null)
                throw new InvalidOperationException("Field 'DynamicILInfo.m_scope' is not found");
            m_scopeExtractor = FieldsExtractor.GetExtractor<DynamicILInfo, object>(m_scopeField);
        }

        public DynamicILInfoWrapper(DynamicILInfo inst)
        {
            this.inst = inst;
        }

        public int m_methodSignature { get { return m_methodSignatureExtractor(inst); } }
        public DynamicScope m_scope { get { return new DynamicScope(m_scopeExtractor(inst)); } }

        public static void Init()
        {
        }

        public MetadataToken GetTokenFor(OpCode opCode, object value)
        {
            if(value is MetadataToken)
                return (MetadataToken)value;
            if(value is MethodBase)
                return GetTokenForMethod((MethodBase)value, opCode);
            if(value is FieldInfo)
                return new MetadataToken((uint)inst.GetTokenFor(((FieldInfo)value).FieldHandle));
            if(value is Type)
                return new MetadataToken((uint)inst.GetTokenFor(((Type)value).TypeHandle));
            if(value is byte[])
                return new MetadataToken((uint)inst.GetTokenFor((byte[])value));
            if(value is string)
                return new MetadataToken((uint)inst.GetTokenFor((string)value));
            throw new InvalidOperationException(string.Format("Unable to build token for {0}", value.GetType()));
        }

        private MetadataToken GetTokenForMethod(MethodBase methodBase, OpCode opcode)
        {
            if(opcode == OpCodes.Call || opcode == OpCodes.Callvirt)
                return m_scope.GetTokenFor(methodBase, MetadataExtensions.BuildMemberRefSignature(methodBase));
            return new MetadataToken((uint)inst.GetTokenFor(methodBase.MethodHandle));
        }

        public DynamicILInfo inst;

        private static readonly Func<DynamicILInfo, int> m_methodSignatureExtractor;
        private static readonly Func<DynamicILInfo, object> m_scopeExtractor;
    }
}