using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class BrStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            var label = ((LabelILInstructionParameter)parameter).Label;
            SaveOrCheck(il, stack, label);
        }
    }
}