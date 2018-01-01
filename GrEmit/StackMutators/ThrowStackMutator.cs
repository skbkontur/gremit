using System;

namespace GrEmit.StackMutators
{
    internal class ThrowStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            CheckNotEmpty(il, stack, () => "An exception must be put onto the evaluation stack in order to perform the 'throw' instruction");
            CheckCanBeAssigned(il, typeof(Exception), stack.Pop());
        }
    }
}