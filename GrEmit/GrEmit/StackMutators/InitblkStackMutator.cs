namespace GrEmit.StackMutators
{
    internal class InitblkStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            CheckNotEmpty(il, stack);
            CheckIsAPointer(il, stack.Pop());
            CheckNotEmpty(il, stack);
            CheckCanBeAssigned(il, typeof(int), stack.Pop());
            CheckNotEmpty(il, stack);
            CheckCanBeAssigned(il, typeof(int), stack.Pop());
        }
    }
}