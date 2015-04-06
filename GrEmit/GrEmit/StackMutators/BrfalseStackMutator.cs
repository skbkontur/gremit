using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class BrfalseStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            var label = ((LabelILInstructionParameter)parameter).Label;
            CheckNotEmpty(il, stack);
            CheckNotStruct(il, stack.Pop());

            SaveOrCheck(il, stack, label);
        }
    }
}