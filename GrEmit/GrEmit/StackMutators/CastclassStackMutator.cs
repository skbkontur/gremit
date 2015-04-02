using System;
using System.Collections.Generic;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class CastclassStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            var type = ((TypeILInstructionParameter)parameter).Type;
            CheckNotEmpty(il, stack);
            CheckCanBeAssigned(il, stack.Pop(), type);
            stack.Push(type);
        }
    }
}