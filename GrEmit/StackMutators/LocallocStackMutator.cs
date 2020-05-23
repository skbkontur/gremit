using System;

namespace GrEmit.StackMutators
{
    internal class LocallocStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            CheckNotEmpty(il, stack, () => $"In order to perform the instruction 'localloc' size must be loaded onto the evaluation stack");
            var type = stack.Pop();
            if (ToCLIType(type) != CLIType.NativeInt)
                ThrowError(il, $"Unable to perform the instruction 'localloc' on type '{type}'");
            stack.Push(typeof(UIntPtr));
        }
    }
}