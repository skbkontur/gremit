using System;
using System.Collections.Generic;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class LdlocaStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            var local = ((LocalILInstructionParameter)parameter).Local;
            stack.Push(local.Type.MakeByRefType());
        }
    }
}