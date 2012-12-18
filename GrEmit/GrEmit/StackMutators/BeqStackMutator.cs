using System;
using System.Collections.Generic;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class BeqStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            var label = ((LabelILInstructionParameter)parameter).Label;
            CheckNotEmpty(il, stack);
            CheckNotStruct(il, stack.Pop());
            CheckNotEmpty(il, stack);
            CheckNotStruct(il, stack.Pop());

            SaveOrCheck(il, stack, label);
        }
    }
}