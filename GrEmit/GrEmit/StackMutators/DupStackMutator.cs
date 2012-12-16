using System;
using System.Collections.Generic;

namespace GrEmit.StackMutators
{
    internal class DupStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            CheckNotEmpty(stack);
            var peek = stack.Peek();
            CheckNotStruct(peek);
            stack.Push(peek);
        }
    }
}