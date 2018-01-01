using System;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class BoxStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            var type = ((TypeILInstructionParameter)parameter).Type;
            CheckNotEmpty(il, stack, () => "To perform the 'box' instruction load a value on the evaluation stack");
            CheckCanBeAssigned(il, type, stack.Pop());
            stack.Push(type.IsEnum ? typeof(Enum) : typeof(object));
        }
    }
}