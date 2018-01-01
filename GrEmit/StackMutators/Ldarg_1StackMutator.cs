namespace GrEmit.StackMutators
{
    internal class Ldarg_1StackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            stack.Push(il.methodParameterTypes[1]);
        }
    }
}