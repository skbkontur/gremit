using System;

namespace GrEmit.StackMutators
{
    internal class JmpStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            if(stack.Count != 0)
                throw new InvalidOperationException("Stack must be empty in order to perform 'jmp' instruction\r\n" + il.GetILCode());
        }
    }
}