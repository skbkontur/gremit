namespace GrEmit.StackMutators
{
    internal class CpobjStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            CheckNotEmpty(il, stack, () => "In order to perform the 'cpobj' instruction a source address must be loaded onto the evaluation stack");
            CheckIsAPointer(il, stack.Pop());
            CheckNotEmpty(il, stack, () => "In order to perform the 'cpobj' instruction a destination address must be loaded onto the evaluation stack");
            CheckIsAPointer(il, stack.Pop());
        }
    }
}