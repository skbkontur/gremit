using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace GrEmit.MethodBodyParsing
{
    internal class MethodBodyOnMethodBase : MethodBody
    {
        public MethodBodyOnMethodBase(MethodBase method, bool resolveTokens)
            : base(GetMethodSignature(method), resolveTokens)
        {
            module = method.Module;
            var methodBody = method.GetMethodBody();
            MaxStack = methodBody.MaxStackSize;
            InitLocals = methodBody.InitLocals;

            var localSignature = methodBody.LocalSignatureMetadataToken != 0
                                     ? method.Module.ResolveSignature(methodBody.LocalSignatureMetadataToken)
                                     : SignatureHelper.GetLocalVarSigHelper().GetSignature(); // null is invalid value
            SetLocalSignature(localSignature);

            ILCodeReader.Read(methodBody.GetILAsByteArray(), ResolveToken, resolveTokens, this);

            ReadExceptions(methodBody.ExceptionHandlingClauses);
        }

        protected override object ResolveToken(MetadataToken token)
        {
            return module.Resolve(token);
        }

        private static byte[] GetMethodSignature(MethodBase method)
        {
            return method.Module.ResolveSignature(method.MetadataToken);
        }

        private void ReadExceptions(IList<ExceptionHandlingClause> exceptionClauses)
        {
            foreach(var exceptionClause in exceptionClauses)
            {
                var handler = new ExceptionHandler((ExceptionHandlerType)exceptionClause.Flags);

                handler.TryStart = GetInstruction(exceptionClause.TryOffset);
                handler.TryEnd = GetInstruction(handler.TryStart.Offset + exceptionClause.TryLength);

                handler.HandlerStart = GetInstruction(exceptionClause.HandlerOffset);
                handler.HandlerEnd = GetInstruction(handler.HandlerStart.Offset + exceptionClause.HandlerLength);

                switch(handler.HandlerType)
                {
                case ExceptionHandlerType.Catch:
                    var token = new MetadataToken((uint)exceptionClause.CatchType.MetadataToken);
                    handler.CatchType = resolveTokens ? ResolveToken(token) : token;
                    break;
                case ExceptionHandlerType.Filter:
                    handler.FilterStart = GetInstruction(exceptionClause.FilterOffset);
                    break;
                }

                ExceptionHandlers.Add(handler);
            }
        }

        private readonly Module module;
    }
}