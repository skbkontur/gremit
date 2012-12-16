using System;
using System.Collections.Generic;

namespace GrEmit.StackMutators
{
    internal class Ldc_I8StackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            stack.Push(typeof(long));
        }
    }
}