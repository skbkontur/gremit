using System;
using System.Collections.Generic;

namespace GrEmit.StackMutators
{
    internal class MathOpStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            CheckNotEmpty(stack);
            CheckNotStruct(stack.Pop());
            CheckNotEmpty(stack);
            Type peek = stack.Pop();
            CheckNotStruct(peek);
            stack.Push(peek);
        }
    }
}