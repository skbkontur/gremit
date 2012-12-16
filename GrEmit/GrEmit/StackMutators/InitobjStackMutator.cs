using System;
using System.Collections.Generic;

namespace GrEmit.StackMutators
{
    internal class InitobjStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            CheckNotEmpty(stack);
            CheckIsAddress(stack.Pop());
        }
    }
}