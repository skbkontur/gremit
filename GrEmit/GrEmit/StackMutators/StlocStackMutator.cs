using System;
using System.Collections.Generic;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class StlocStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            var local = ((LocalILInstructionParameter)parameter).Local;
            CheckNotEmpty(il, stack);
            var peek = stack.Pop();
            CheckCanBeAssigned(il, local.Type, peek);
        }
    }
}