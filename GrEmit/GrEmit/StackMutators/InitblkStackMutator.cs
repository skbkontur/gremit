using System;
using System.Collections.Generic;

namespace GrEmit.StackMutators
{
    internal class InitblkStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            CheckNotEmpty(il, stack);
            CheckIsAPointer(il, stack.Pop());
            CheckNotEmpty(il, stack);
            CheckCanBeAssigned(il, typeof(int), stack.Pop());
            CheckNotEmpty(il, stack);
            CheckCanBeAssigned(il, typeof(int), stack.Pop());
        }
    }
}