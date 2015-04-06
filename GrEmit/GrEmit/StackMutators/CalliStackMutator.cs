using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class CalliStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            var calliParameter = (MethodByAddressILInstructionParameter)parameter;
            var returnType = calliParameter.ReturnType;
            var parameterTypes = calliParameter.ParameterTypes;
            CheckNotEmpty(il, stack);
            var entryPoint = stack.Pop();
            if(ToCLIType(entryPoint) != CLIType.NativeInt)
                ThrowError(il, string.Format("An entry point must be a native int but was '{0}'", entryPoint));
            for(var i = parameterTypes.Length - 1; i >= 0; --i)
            {
                CheckNotEmpty(il, stack);
                CheckCanBeAssigned(il, parameterTypes[i], stack.Pop());
            }
            if(returnType != typeof(void))
                stack.Push(returnType);
        }
    }
}