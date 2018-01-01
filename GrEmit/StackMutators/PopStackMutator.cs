namespace GrEmit.StackMutators
{
    internal class PopStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            CheckNotEmpty(il, stack, () => "Stack cannot be empty in order to perform the 'pop' instruction");
            stack.Pop();
        }
    }
}