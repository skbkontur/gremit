namespace GrEmit.StackMutators
{
    internal class Ldc_R8StackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            stack.Push(typeof(double));
        }
    }
}