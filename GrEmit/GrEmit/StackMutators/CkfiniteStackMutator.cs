using System;
using System.Collections.Generic;

namespace GrEmit.StackMutators
{
    internal class CkfiniteStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            CheckNotEmpty(il, stack);
            var peek = stack.Peek();
            if(ToCLIType(peek) != CLIType.Float)
                ThrowError(il, string.Format("It is only allowed to check if value is finite for floating point values but was '{0}'", peek));
        }
    }
}