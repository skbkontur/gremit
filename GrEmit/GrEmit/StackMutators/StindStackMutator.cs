using System;
using System.Collections.Generic;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class StindStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            var type = ((TypeILInstructionParameter)parameter).Type;
            CheckNotEmpty(stack);
            CheckCanBeAssigned(type, stack.Pop());
            CheckNotEmpty(stack);
            CheckIsAddress(stack.Pop());
        }
    }
}