namespace GrEmit.StackMutators
{
    internal class CkfiniteStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            CheckNotEmpty(il, stack, () => "In order to perform the 'ckfinite' instruction an instance must be loaded onto the evaluation stack");
            var peek = stack.Peek();
            if(ToCLIType(peek) != CLIType.Float)
                ThrowError(il, string.Format("It is only allowed to check if value is finite for floating point values but was '{0}'", peek));
        }
    }
}