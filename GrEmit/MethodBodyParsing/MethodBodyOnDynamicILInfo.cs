using System.Reflection.Emit;

namespace GrEmit.MethodBodyParsing
{
    internal class MethodBodyOnDynamicILInfo : MethodBody
    {
        public MethodBodyOnDynamicILInfo(DynamicMethod dynamicMethod, GrEmit.Utils.DynamicILInfo dynamicILInfo, bool resolveTokens)
            : base(GetMethodSignature(dynamicILInfo), resolveTokens)
        {
            scope = new DynamicILInfoWrapper(dynamicILInfo).m_scope;
            using(var dynamicResolver = new DynamicResolver(dynamicMethod, dynamicILInfo))
            {
                int stackSize;
                int initLocals;
                int EHCount;

                var code = dynamicResolver.GetCodeInfo(out stackSize, out initLocals, out EHCount);

                MaxStack = stackSize;
                InitLocals = initLocals != 0;

                SetLocalSignature(dynamicResolver.m_localSignature);

                ILCodeReader.Read(code, ResolveToken, resolveTokens, this);

                ExceptionsInfoReader.Read(dynamicResolver.GetRawEHInfo(), ResolveToken, resolveTokens, this);
            }
        }

        protected override object ResolveToken(MetadataToken token)
        {
            return DynamicMethodHelpers.Resolve(scope[token.ToInt32()]);
        }

        private static byte[] GetMethodSignature(GrEmit.Utils.DynamicILInfo dynamicILInfo)
        {
            var wrapper = new DynamicILInfoWrapper(dynamicILInfo);
            return (byte[])wrapper.m_scope[wrapper.m_methodSignature];
        }

        private readonly DynamicScope scope;
    }
}