namespace GrEmit.StackMutators
{
    internal class DupStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            CheckNotEmpty(il, stack, () => "Stack cannot be empty in order to perform the 'dup' instruction");
            stack.Push(stack.Peek());
        }
    }
}