using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace GrEmit.MethodBodyParsing
{
    public static class MetadataExtensions
    {
        public static object Resolve(this Module module, MetadataToken token)
        {
            switch(token.TokenType)
            {
            case TokenType.Method:
            case TokenType.MethodSpec:
                return module.ResolveMethod(token.ToInt32(), universalArguments, universalArguments);
            case TokenType.MemberRef:
                return module.ResolveMember(token.ToInt32(), universalArguments, universalArguments);
            case TokenType.Field:
                return module.ResolveField(token.ToInt32(), universalArguments, universalArguments);
            case TokenType.TypeDef:
            case TokenType.TypeRef:
                return module.ResolveType(token.ToInt32(), universalArguments, universalArguments);
            case TokenType.Signature:
                return module.ResolveSignature(token.ToInt32());
            case TokenType.String:
                return module.ResolveString(token.ToInt32());
            default:
                throw new NotSupportedException();
            }
        }

        public static MethodBase ResolveMethod(Module module, MetadataToken token)
        {
            switch(token.TokenType)
            {
            case TokenType.MethodSpec:
            case TokenType.Method:
                return module.ResolveMethod(token.ToInt32(), universalArguments, universalArguments);
            case TokenType.MemberRef:
                var member = module.ResolveMember(token.ToInt32(), universalArguments, universalArguments);
                switch(member.MemberType)
                {
                case MemberTypes.Constructor:
                case MemberTypes.Method:
                    return (MethodBase)member;
                default:
                    return null;
                }
            default:
                return null;
            }
        }

        public static SignatureHelper BuildMemberRefSignature(MethodBase methodBase)
        {
            return BuildMemberRefSignature(methodBase.CallingConvention,
                                           GetReturnType(methodBase),
                                           methodBase.GetParameters().Select(p => p.ParameterType).ToArray(),
                                           null);
        }

        private static SignatureHelper BuildMemberRefSignature(
            CallingConventions call,
            Type returnType,
            Type[] parameterTypes,
            Type[] optionalParameterTypes)
        {
            var sig = SignatureHelper.GetMethodSigHelper(call, returnType);
            if(parameterTypes != null)
            {
                foreach(var parameterType in parameterTypes)
                    sig.AddArgument(parameterType);
            }
            if(optionalParameterTypes != null && optionalParameterTypes.Length != 0)
            {
                // add the sentinel 
                sig.AddSentinel();
                foreach(var optionalParameterType in optionalParameterTypes)
                    sig.AddArgument(optionalParameterType);
            }
            return sig;
        }

        private static Type GetReturnType(MethodBase methodBase)
        {
            var methodInfo = methodBase as MethodInfo;
            if(methodInfo != null)
                return methodInfo.ReturnType;
            if(methodBase is ConstructorInfo)
                return typeof(void);
            throw new InvalidOperationException(string.Format("{0} is not supported", methodBase.GetType()));
        }

        private static readonly Type __Canon = typeof(object).Assembly.GetType("System.__Canon");
        private static readonly Type[] universalArguments = Enumerable.Repeat(__Canon, 1024).ToArray();
    }
}