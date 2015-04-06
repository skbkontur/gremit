using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class StargStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            var index = (int)((PrimitiveILInstructionParameter)parameter).Value;
            CheckNotEmpty(il, stack);
            CheckCanBeAssigned(il, il.methodParameterTypes[index], stack.Pop());
        }
    }
}