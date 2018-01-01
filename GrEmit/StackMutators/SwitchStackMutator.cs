using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class SwitchStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            var labels = ((LabelsILInstructionParameter)parameter).Labels;
            CheckNotEmpty(il, stack, () => "A value must be put onto the evaluation stack in order to perform the 'switch' instruction");
            CheckNotStruct(il, stack.Pop());
            foreach(var label in labels)
                SaveOrCheck(il, stack, label);
        }
    }
}