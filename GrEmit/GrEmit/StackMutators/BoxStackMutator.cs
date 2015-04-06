using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class BoxStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            var type = ((TypeILInstructionParameter)parameter).Type;
            CheckNotEmpty(il, stack);
            CheckCanBeAssigned(il, type, stack.Pop());
            stack.Push(typeof(object));
        }
    }
}