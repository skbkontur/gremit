using System;

namespace GrEmit.StackMutators
{
    internal class LdftnStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            stack.Push(typeof(IntPtr));
        }
    }
}