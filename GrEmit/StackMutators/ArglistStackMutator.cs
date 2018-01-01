using System;

namespace GrEmit.StackMutators
{
    internal class ArglistStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            stack.Push(typeof(IntPtr));
        }
    }
}