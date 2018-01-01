using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class IsinstStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            var type = ((TypeILInstructionParameter)parameter).Type;
            CheckNotEmpty(il, stack, () => "In order to perform the 'isinst' instruction an instance must be put onto the evaluation stack");
            CheckCanBeAssigned(il, typeof(object), stack.Pop());
            stack.Push(type.IsValueType ? typeof(object) : type);
        }
    }
}