using System;
using System.Collections.Generic;

namespace GrEmit.StackMutators
{
    internal class NegStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            CheckNotEmpty(il, stack);
            CheckNotStruct(il, stack.Peek());
        }
    }
}