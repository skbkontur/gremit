using System;
using System.Collections.Generic;

namespace GrEmit.StackMutators
{
    internal class MathOpStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            CheckNotEmpty(il, stack);
            Type peek = stack.Pop();
            CheckNotStruct(il, peek);
            CheckNotEmpty(il, stack);
            CheckNotStruct(il, stack.Pop());
            stack.Push(peek);
        }
    }
}