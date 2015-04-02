using System;
using System.Collections.Generic;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class MarkLabelStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            var label = ((LabelILInstructionParameter)parameter).Label;
            if(stack != null)
                SaveOrCheck(il, stack, label);
            Type[] labelStack;
            if(il.labelStacks.TryGetValue(label, out labelStack))
                stack = new Stack<Type>(labelStack);
        }
    }
}