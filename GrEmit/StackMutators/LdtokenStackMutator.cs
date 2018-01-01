using System;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class LdtokenStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            if(parameter is TypeILInstructionParameter)
                stack.Push(typeof(RuntimeTypeHandle));
            if(parameter is MethodILInstructionParameter)
                stack.Push(typeof(RuntimeMethodHandle));
            if(parameter is FieldILInstructionParameter)
                stack.Push(typeof(RuntimeFieldHandle));
        }
    }
}