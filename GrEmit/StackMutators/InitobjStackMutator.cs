namespace GrEmit.StackMutators
{
    internal class InitobjStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            CheckNotEmpty(il, stack, () => "In order to perform the 'initobj' instruction an address must be loaded onto the evaluation stack");
            CheckIsAPointer(il, stack.Pop());
        }
    }
}