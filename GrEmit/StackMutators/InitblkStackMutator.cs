namespace GrEmit.StackMutators
{
    internal class InitblkStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            CheckNotEmpty(il, stack, () => "In order to perform the 'initblk' instruction a number of bytes to initialize must be put onto the evaluation stack");
            CheckCanBeAssigned(il, typeof(int), stack.Pop());
            CheckNotEmpty(il, stack, () => "In order to perform the 'initblk' instruction an initialization value must be put onto the evaluation stack");
            CheckCanBeAssigned(il, typeof(int), stack.Pop());
            CheckNotEmpty(il, stack, () => "In order to perform the 'initblk' instruction a starting address must be put onto the evaluation stack");
            CheckIsAPointer(il, stack.Pop());
        }
    }
}