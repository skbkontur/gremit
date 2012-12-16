using System;
using System.Collections.Generic;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class BleStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            var label = ((LabelILInstructionParameter)parameter).Label;
            CheckNotEmpty(stack);
            CheckNotStruct(stack.Pop());
            CheckNotEmpty(stack);
            CheckNotStruct(stack.Pop());

            SaveOrCheck(il, stack, label);
        }
    }
}