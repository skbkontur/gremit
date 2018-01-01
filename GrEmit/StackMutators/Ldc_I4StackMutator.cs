namespace GrEmit.StackMutators
{
    internal class Ldc_I4StackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            stack.Push(typeof(int));
        }
    }
}