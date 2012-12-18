using System;
using System.Collections.Generic;

namespace GrEmit.StackMutators
{
    internal class DupStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            CheckNotEmpty(il, stack);
            var peek = stack.Peek();
            CheckNotStruct(il, peek);
            stack.Push(peek);
        }
    }
}