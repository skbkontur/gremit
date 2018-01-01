using System;
using System.Reflection;

namespace GrEmit.MethodBodyParsing
{
    internal class DynamicILInfoWrapper
    {
        static DynamicILInfoWrapper()
        {
            var m_methodSignatureField = typeof(GrEmit.Utils.DynamicILInfo).GetField("m_methodSignature", BindingFlags.Instance | BindingFlags.NonPublic);
            if(m_methodSignatureField == null)
                throw new InvalidOperationException("Field 'DynamicILInfo.m_methodSignature' is not found");
            m_methodSignatureExtractor = FieldsExtractor.GetExtractor<GrEmit.Utils.DynamicILInfo, int>(m_methodSignatureField);

            var m_scopeField = typeof(GrEmit.Utils.DynamicILInfo).GetField("m_scope", BindingFlags.Instance | BindingFlags.NonPublic);
            if(m_scopeField == null)
                throw new InvalidOperationException("Field 'DynamicILInfo.m_scope' is not found");
            m_scopeExtractor = FieldsExtractor.GetExtractor<GrEmit.Utils.DynamicILInfo, object>(m_scopeField);
        }

        public DynamicILInfoWrapper(GrEmit.Utils.DynamicILInfo inst)
        {
            this.inst = inst;
        }

        public int m_methodSignature => m_methodSignatureExtractor(inst);
        public DynamicScope m_scope => new DynamicScope(m_scopeExtractor(inst));

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
            {
                var field = (FieldInfo)value;
                if(field.DeclaringType != null && field.DeclaringType.IsGenericType)
                    return new MetadataToken((uint)inst.GetTokenFor(field.FieldHandle, field.DeclaringType.TypeHandle));
                return new MetadataToken((uint)inst.GetTokenFor(field.FieldHandle));
            }
            if(value is Type)
                return new MetadataToken((uint)inst.GetTokenFor(((Type)value).TypeHandle));
            if(value is byte[])
                return new MetadataToken((uint)inst.GetTokenFor((byte[])value));
            if(value is string)
                return new MetadataToken((uint)inst.GetTokenFor((string)value));
            throw new InvalidOperationException($"Unable to build token for {value.GetType()}");
        }

        private MetadataToken GetTokenForMethod(MethodBase methodBase, OpCode opcode)
        {
            if(opcode == OpCodes.Call || opcode == OpCodes.Callvirt)
                return m_scope.GetTokenFor(methodBase, MetadataExtensions.BuildMemberRefSignature(methodBase));
            if(methodBase.DeclaringType != null && methodBase.DeclaringType.IsGenericType)
                return new MetadataToken((uint)inst.GetTokenFor(methodBase.MethodHandle, methodBase.DeclaringType.TypeHandle));
            return new MetadataToken((uint)inst.GetTokenFor(methodBase.MethodHandle));
        }

        public GrEmit.Utils.DynamicILInfo inst;

        private static readonly Func<GrEmit.Utils.DynamicILInfo, int> m_methodSignatureExtractor;
        private static readonly Func<GrEmit.Utils.DynamicILInfo, object> m_scopeExtractor;
    }
}