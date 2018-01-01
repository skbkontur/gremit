using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class Unbox_AnyStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            var type = ((TypeILInstructionParameter)parameter).Type;
            CheckNotEmpty(il, stack, () => "An object must be put onto the evaluation stack in order to perform the 'unbox_any' instruction");
            CheckCanBeAssigned(il, typeof(object), stack.Pop());
            stack.Push(type);
        }
    }
}