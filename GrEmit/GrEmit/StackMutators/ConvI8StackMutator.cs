using System;
using System.Collections.Generic;

namespace GrEmit.StackMutators
{
    internal class ConvI8StackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            CheckNotEmpty(il, stack);
            CheckNotStruct(il, stack.Pop());
            stack.Push(typeof(long));
        }
    }
}