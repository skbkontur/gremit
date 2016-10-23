using System.Reflection.Emit;

namespace GrEmit.MethodBodyParsing
{
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
            return scope[token.ToInt32()];
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