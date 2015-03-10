using System;
using System.Collections.Generic;

namespace GrEmit.StackMutators
{
    internal class CpobjStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            CheckNotEmpty(il, stack);
            CheckIsAddress(il, stack.Pop());
            CheckNotEmpty(il, stack);
            CheckIsAddress(il, stack.Pop());
        }
    }
}