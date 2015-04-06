using System;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class StelemStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            var elementType = ((TypeILInstructionParameter)parameter).Type;
            CheckNotEmpty(il, stack);
            CheckCanBeAssigned(il, elementType, stack.Pop());
            CheckNotEmpty(il, stack);
            CheckCanBeAssigned(il, typeof(int), stack.Pop());
            CheckNotEmpty(il, stack);
            var esType = stack.Pop();
            var array = esType.ToType();
            if(!array.IsArray && array != typeof(Array))
                ThrowError(il, string.Format("An array expected but was '{0}'", esType));
        }
    }
}