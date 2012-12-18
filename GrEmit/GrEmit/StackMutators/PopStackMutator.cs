using System;
using System.Collections.Generic;

namespace GrEmit.StackMutators
{
    internal class PopStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            CheckNotEmpty(il, stack);
            CheckNotStruct(il, stack.Pop());
        }
    }
}