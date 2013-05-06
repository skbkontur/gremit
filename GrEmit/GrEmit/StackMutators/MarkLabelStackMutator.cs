using System;
using System.Collections.Generic;
using System.Linq;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class MarkLabelStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            var label = ((LabelILInstructionParameter)parameter).Label;
            if(stack == null)
            {
                Type[] labelStack;
                if(il.labelStacks.TryGetValue(label, out labelStack))
                    stack = new Stack<Type>(labelStack);
            }
            else
            {
                Type[] labelStack;
                if(il.labelStacks.TryGetValue(label, out labelStack))
                    CheckStacksEqual(il, label, stack, labelStack);
                else
                    il.labelStacks.Add(label, stack.Reverse().ToArray());
            }
        }
    }
}