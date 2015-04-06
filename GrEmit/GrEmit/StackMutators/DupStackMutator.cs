namespace GrEmit.StackMutators
{
    internal class DupStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            CheckNotEmpty(il, stack);
            stack.Push(stack.Peek());
        }
    }
}