using System;

namespace GrEmit.StackMutators
{
    internal class ThrowStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            CheckNotEmpty(il, stack);
            CheckCanBeAssigned(il, typeof(Exception), stack.Pop());
        }
    }
}