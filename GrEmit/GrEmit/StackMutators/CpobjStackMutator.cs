namespace GrEmit.StackMutators
{
    internal class CpobjStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            CheckNotEmpty(il, stack);
            CheckIsAPointer(il, stack.Pop());
            CheckNotEmpty(il, stack);
            CheckIsAPointer(il, stack.Pop());
        }
    }
}