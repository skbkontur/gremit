using System;
using System.Collections.Generic;

namespace GrEmit.StackMutators
{
    internal class Ldarg_0StackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            stack.Push(il.methodParameterTypes[0]);
        }
    }
}