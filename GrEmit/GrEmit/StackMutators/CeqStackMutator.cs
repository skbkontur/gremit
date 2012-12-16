using System;
using System.Collections.Generic;

namespace GrEmit.StackMutators
{
    internal class CeqStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            CheckNotEmpty(stack);
            CheckNotStruct(stack.Pop());
            CheckNotEmpty(stack);
            CheckNotStruct(stack.Pop());
            stack.Push(typeof(int));
        }
    }
}