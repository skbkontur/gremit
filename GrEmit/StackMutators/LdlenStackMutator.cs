using System;

namespace GrEmit.StackMutators
{
    internal class LdlenStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            CheckNotEmpty(il, stack, () => "In order to perform the 'ldlen' instruction an array must be loaded onto the evaluation stack");
            var esType = stack.Pop();
            var array = esType.ToType();
            if(!array.IsArray && array != typeof(Array))
                ThrowError(il, string.Format("An array expected but was '{0}'", esType));
            stack.Push(typeof(int));
        }
    }
}