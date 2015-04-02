using System;
using System.Collections.Generic;

namespace GrEmit.StackMutators
{
    internal class RetStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            if(il.methodReturnType == typeof(void))
            {
                if(stack.Count != 0)
                    ThrowError(il, "The function being emitted is void. Thus at the end stack must be empty");
            }
            else if(stack.Count == 0)
                ThrowError(il, "The function being emitted is not void. But stack is empty");
            else if(stack.Count > 1)
                ThrowError(il, "At the end stack must contain exactly one element");
            else
            {
                var peek = stack.Pop();
                CheckCanBeAssigned(il, il.methodReturnType, peek);
            }
        }
    }
}