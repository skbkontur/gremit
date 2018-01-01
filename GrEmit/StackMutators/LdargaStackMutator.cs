using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class LdargaStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            var index = (int)((PrimitiveILInstructionParameter)parameter).Value;
            stack.Push(il.methodParameterTypes[index].MakeByRefType());
        }
    }
}