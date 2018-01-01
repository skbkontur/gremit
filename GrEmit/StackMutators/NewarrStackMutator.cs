using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class NewarrStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            var type = ((TypeILInstructionParameter)parameter).Type;
            CheckNotEmpty(il, stack, () => "In order to perform the 'newarr' instruction a length of an array must be loaded onto the evaluation stack");
            CheckCanBeAssigned(il, typeof(int), stack.Pop());
            stack.Push(type.MakeArrayType());
        }
    }
}