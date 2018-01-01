using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class MarkLabelStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            var label = ((LabelILInstructionParameter)parameter).Label;
            if(stack != null)
                SaveOrCheck(il, stack, label);
            ESType[] labelStack;
            if(il.labelStacks.TryGetValue(label, out labelStack))
                stack = new EvaluationStack(labelStack);
        }
    }
}