using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class Starg_SStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            var index = (byte)((PrimitiveILInstructionParameter)parameter).Value;
            CheckNotEmpty(il, stack, () => "A value must be put onto the evaluation stack in order to perform the 'starg' instruction");
            CheckCanBeAssigned(il, il.methodParameterTypes[index], stack.Pop());
        }
    }
}