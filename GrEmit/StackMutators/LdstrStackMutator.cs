namespace GrEmit.StackMutators
{
    internal class LdstrStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            stack.Push(typeof(string));
        }
    }
}