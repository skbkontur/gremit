using System;
using System.Collections.Generic;

namespace GrEmit.StackMutators
{
    internal class ThrowStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            CheckNotEmpty(il, stack);
            CheckCanBeAssigned(il, typeof(object), stack.Pop());
        }
    }
}