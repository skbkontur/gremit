using System;
using System.Collections.Generic;

namespace GrEmit.StackMutators
{
    internal class RemStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            CheckNotEmpty(il, stack);
            Type firstOperand = stack.Pop();
            CheckNotStruct(il, firstOperand);
            CheckNotEmpty(il, stack);
            var secondOperand = stack.Pop();
            CheckNotStruct(il, secondOperand);
            stack.Push(firstOperand);
        }
    }
}