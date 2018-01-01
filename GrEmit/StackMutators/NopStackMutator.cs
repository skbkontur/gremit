namespace GrEmit.StackMutators
{
    internal class NopStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
        }
    }
}