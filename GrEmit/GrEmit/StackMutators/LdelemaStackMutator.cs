using System;
using System.Collections.Generic;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class LdelemaStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            var elementType = ((TypeILInstructionParameter)parameter).Type;
            CheckNotEmpty(il, stack);
            CheckCanBeAssigned(il, typeof(int), stack.Pop());
            CheckNotEmpty(il, stack);
            Type peek = stack.Pop();
            if(!peek.IsArray)
                throw new InvalidOperationException("An array expected but was '" + peek + "'");
            stack.Push(elementType.MakeByRefType());
        }
    }
}