using System;
using System.Collections.Generic;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class SwitchStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            var labels = ((LabelsILInstructionParameter)parameter).Labels;
            CheckNotEmpty(il, stack);
            CheckNotStruct(il, stack.Pop());
            foreach(var label in labels)
                SaveOrCheck(il, stack, label);
        }
    }
}