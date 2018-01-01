namespace GrEmit.StackMutators
{
    internal class LdnullStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            stack.Push(ESType.Zero);
        }
    }
}