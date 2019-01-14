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
            CheckNotEmpty(il, stack, () => "In order to perform the 'calli' instruction an entry point must be loaded onto the evaluation stack");
            var entryPoint = stack.Pop();
            if (ToCLIType(entryPoint) != CLIType.NativeInt)
                ThrowError(il, $"An entry point must be a native int but was '{entryPoint}'");
            for (var i = parameterTypes.Length - 1; i >= 0; --i)
            {
                CheckNotEmpty(il, stack, () => $"Expected exactly {parameterTypes.Length} parameters, but the evaluation stack is empty");
                CheckCanBeAssigned(il, parameterTypes[i], stack.Pop());
            }
            if (returnType != typeof(void))
                stack.Push(returnType);
        }
    }
}