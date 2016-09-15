using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace GrEmit.MethodBodyParsing
{
    public abstract class MethodBody
    {
        protected MethodBody(byte[] methodSignature, bool resolveTokens)
        {
            this.resolveTokens = resolveTokens;
            Instructions = new InstructionCollection();
            ExceptionHandlers = new Collection<ExceptionHandler>();
            MethodSignature = methodSignature;
        }

        protected abstract object ResolveToken(MetadataToken token);

        public static unsafe MethodBody Read(byte* rawMethodBody, Module module, MetadataToken methodSignatureToken, bool resolveTokens)
        {
            return new MethodBodyOnUnmanagedBuffer(rawMethodBody, module, methodSignatureToken, resolveTokens);
        }

        public static MethodBody Read(MethodBase method, bool resolveTokens)
        {
            return new MethodBodyOnMethodBase(method, resolveTokens);
        }

        public static unsafe MethodBody Read(DynamicMethod dynamicMethod, bool resolveTokens)
        {
            var wrapper = new DynamicMethodWrapper(dynamicMethod);
            var dynamicILInfo = wrapper.m_DynamicILInfo;
            if(dynamicILInfo != null)
                return new MethodBodyOnDynamicILInfo(dynamicMethod, dynamicILInfo, resolveTokens);
            var ilGenerator = wrapper.m_ilGenerator;
            if(ilGenerator != null)
                return new MethodBodyOnDynamicILGenerator(dynamicMethod, ilGenerator, resolveTokens);
            return new MethodBodyOnUnmanagedBuffer(null, null, MetadataToken.Zero, resolveTokens);
        }

        protected Instruction GetInstruction(int offset)
        {
            return Instructions.GetInstruction(offset);
        }

        public int MaxStack { get; set; }

        public bool InitLocals { get; set; }

        internal MetadataToken LocalVarToken { get; set; }

        public byte[] MethodSignature { get; private set; }

        public bool HasExceptionHandlers { get { return !ExceptionHandlers.IsNullOrEmpty(); } }

        protected void SetLocalSignature(byte[] localSignature)
        {
            localVarSigBuilder = new LocalVarSigBuilder(localSignature);
        }

        public LocalInfo AddLocalVariable(byte[] signature)
        {
            if(localVarSigBuilder == null)
                localVarSigBuilder = new LocalVarSigBuilder();
            return localVarSigBuilder.AddLocalVariable(signature);
        }

        public LocalInfo AddLocalVariable(Type localType, bool isPinned = false)
        {
            if(localVarSigBuilder == null)
                localVarSigBuilder = new LocalVarSigBuilder();
            return localVarSigBuilder.AddLocalVariable(localType, isPinned);
        }

        public byte[] GetLocalSignature()
        {
            if(localVarSigBuilder == null)
                localVarSigBuilder = new LocalVarSigBuilder();
            return localVarSigBuilder.GetSignature();
        }

        public int LocalVariablesCount()
        {
            if(localVarSigBuilder == null)
                localVarSigBuilder = new LocalVarSigBuilder();
            return localVarSigBuilder.Count;
        }

        public void WriteToDynamicMethod(DynamicMethod dynamicMethod, int? maxStack)
        {
            Seal();
            MaxStack = maxStack ?? new MaxStackSizeCalculator(this, ResolveToken).ComputeMaxStack();
            var dynamicILInfo = new DynamicMethodWrapper(dynamicMethod).GetDynamicILInfoWithOldScope();
            var wrapper = new DynamicILInfoWrapper(dynamicILInfo);
            var code = new ILCodeBaker(Instructions, wrapper.GetTokenFor).BakeILCode();
            dynamicILInfo.SetCode(code, MaxStack);
            dynamicILInfo.SetLocalSignature(GetLocalSignature());
            if(HasExceptionHandlers)
                dynamicILInfo.SetExceptions(new ExceptionsBaker(ExceptionHandlers, Instructions, wrapper.GetTokenFor).BakeExceptions());
        }

        public byte[] GetFullMethodBody(Func<byte[], MetadataToken> signatureTokenBuilder, int? maxStack)
        {
            if(resolveTokens)
                throw new InvalidOperationException("Token builder must be supplied");
            Seal();
            MaxStack = maxStack ?? new MaxStackSizeCalculator(this, ResolveToken).ComputeMaxStack();
            LocalVarToken = GetVariablesSignature(signatureTokenBuilder);
            return new FullMethodBodyBaker(this, null).BakeMethodBody();
        }

        public byte[] GetFullMethodBody(Func<OpCode, object, MetadataToken> tokenBuilder, int? maxStack)
        {
            Seal();
            MaxStack = maxStack ?? new MaxStackSizeCalculator(this, ResolveToken).ComputeMaxStack();
            LocalVarToken = GetVariablesSignature(signature => tokenBuilder(default(OpCode), signature));
            return new FullMethodBodyBaker(this, tokenBuilder).BakeMethodBody();
        }

        private MetadataToken GetVariablesSignature(Func<byte[], MetadataToken> signatureTokenBuilder)
        {
            return LocalVariablesCount() == 0 ? MetadataToken.Zero : signatureTokenBuilder(GetLocalSignature());
        }

        public void Seal()
        {
            Instructions.SimplifyMacros();
            Instructions.OptimizeMacros();

            isSealed = true;
        }

        public override string ToString()
        {
            var result = new StringBuilder();

            result.AppendLine("Instructions:");
            foreach(var instruction in Instructions)
                result.AppendLine(instruction.ToString());

            result.AppendLine();

            result.AppendLine("Exception handlers:");
            foreach(var exceptionHandler in ExceptionHandlers)
                result.AppendLine(exceptionHandler.ToString());

            return result.ToString();
        }

        public InstructionCollection Instructions { get; private set; }
        public Collection<ExceptionHandler> ExceptionHandlers { get; private set; }

        public static void Init()
        {
            DynamicMethodWrapper.Init();
            DynamicILGenerator.Init();
            DynamicILInfoWrapper.Init();
            DynamicScope.Init();
            DynamicResolver.Init();
        }

        protected readonly bool resolveTokens;

        private bool isSealed;

        private LocalVarSigBuilder localVarSigBuilder;
    }
}