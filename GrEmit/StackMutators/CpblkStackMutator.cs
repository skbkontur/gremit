namespace GrEmit.StackMutators
{
    internal class CpblkStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            CheckNotEmpty(il, stack, () => "In order to perform the 'cpblk' instruction a number of bytes to copy must be loaded onto the evaluation stack");
            CheckCanBeAssigned(il, typeof(int), stack.Pop());
            CheckNotEmpty(il, stack, () => "In order to perform the 'cpblk' instruction a source address must be loaded onto the evaluation stack");
            CheckIsAPointer(il, stack.Pop());
            CheckNotEmpty(il, stack, () => "In order to perform the 'cpblk' instruction a destination address must be loaded onto the evaluation stack");
            CheckIsAPointer(il, stack.Pop());
        }
    }
}