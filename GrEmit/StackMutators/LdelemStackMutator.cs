using System;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class LdelemStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            var elementType = ((TypeILInstructionParameter)parameter).Type;
            CheckNotEmpty(il, stack, () => "In order to perform the 'ldelem' instruction an index must be put onto the evaluation stack");
            CheckCanBeAssigned(il, typeof(int), stack.Pop());
            CheckNotEmpty(il, stack, () => "In order to perform the 'ldelem' instruction an array must be put onto the evaluation stack");
            var esType = stack.Pop();
            var array = esType.ToType();
            if(!array.IsArray && array != typeof(Array))
                throw new InvalidOperationException(string.Format("An array expected to perform the 'ldelem' instruction but was '{0}'", esType));
            CheckCanBeAssigned(il, elementType, array == typeof(Array) ? typeof(object) : array.GetElementType());
            stack.Push(elementType);
        }
    }
}