using System;
using System.Collections.Generic;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class Ldarga_SStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            var index = (byte)((PrimitiveILInstructionParameter)parameter).Value;
            stack.Push(il.methodParameterTypes[index].MakeByRefType());
        }
    }
}