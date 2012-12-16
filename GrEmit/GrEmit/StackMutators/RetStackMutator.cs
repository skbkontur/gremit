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
                    throw new InvalidOperationException("At the end stack must be empty");
            }
            else if(stack.Count == 0)
                throw new InvalidOperationException("Stack is empty");
            else if(stack.Count > 1)
                throw new InvalidOperationException("At the end stack must contain exactly one element");
            else
            {
                var peek = stack.Pop();
                CheckCanBeAssigned(il.methodReturnType, peek);
            }
        }
    }
}