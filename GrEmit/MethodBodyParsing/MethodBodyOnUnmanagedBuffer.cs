using System.Reflection;

namespace GrEmit.MethodBodyParsing
{
    internal class MethodBodyOnUnmanagedBuffer : MethodBody
    {
        public unsafe MethodBodyOnUnmanagedBuffer(byte* rawMethodBody, Module module, MetadataToken methodSignatureToken, bool resolveTokens)
            : base(GetMethodSignature(module, methodSignatureToken), resolveTokens)
        {
            this.module = module;
            if(rawMethodBody != null)
                Read(rawMethodBody);
        }

        protected override object ResolveToken(MetadataToken token)
        {
            return module.Resolve(token);
        }

        private static byte[] GetMethodSignature(Module module, MetadataToken methodSignatureToken)
        {
            return module == null || methodSignatureToken.RID == 0
                       ? new byte[0]
                       : module.ResolveSignature(methodSignatureToken.ToInt32());
        }

        private unsafe void Read(byte* rawMethodBody)
        {
            var header = new MethodHeaderReader(rawMethodBody).Read();
            MaxStack = header.MaxStack;
            InitLocals = header.InitLocals;
            LocalVarToken = header.LocalVarToken;
            if(LocalVarToken.RID != 0 && module != null)
                SetLocalSignature(module.ResolveSignature(LocalVarToken.ToInt32()));
            new ILCodeReader(rawMethodBody + header.HeaderSize, header.CodeSize, ResolveToken, resolveTokens).Read(this);
            if(header.HasExceptions)
                new ExceptionsInfoReader(rawMethodBody + Align(header.HeaderSize + header.CodeSize, 4), ResolveToken, resolveTokens).Read(this);
        }

        private static int Align(int position, int align)
        {
            align--;
            return (position + align) & ~align;
        }

        private readonly Module module;
    }
}