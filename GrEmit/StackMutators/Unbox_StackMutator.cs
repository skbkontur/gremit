using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class Unbox_StackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            var unboxedValueType = ((TypeILInstructionParameter)parameter).Type;
            if (!unboxedValueType.IsValueType)
            {
                ThrowError(il, $"Unbox may only be used with value types. Passed [{unboxedValueType}]");
            }
            
            CheckNotEmpty(il, stack, () => "An object reference to a value type must be put onto the evaluation stack in order to perform the 'unbox' instruction");
            CheckIsNotValueType(il, stack.Pop()); // The stack should contain a managed reference type, which we will unbox.
            stack.Push(unboxedValueType.MakeByRefType()); // Pushes a reference to the value type onto the stack.
        }
    }
}