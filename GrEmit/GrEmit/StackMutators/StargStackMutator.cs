using System;
using System.Collections.Generic;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class StargStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            var index = (int)((PrimitiveILInstructionParameter)parameter).Value;
            CheckNotEmpty(il, stack);
            CheckCanBeAssigned(il, il.methodParameterTypes[index], stack.Pop());
        }
    }
}