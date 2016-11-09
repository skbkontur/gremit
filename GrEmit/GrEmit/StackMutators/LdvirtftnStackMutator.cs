using System;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class LdvirtftnStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            var method = ((MethodILInstructionParameter)parameter).Method;
            CheckNotEmpty(il, stack, () => "Ldvirtftn requires an instance to be loaded onto evaluation stack");
            var instance = stack.Pop();
            var declaringType = method.DeclaringType;
            CheckCanBeAssigned(il, declaringType, instance);
            stack.Push(typeof(IntPtr));
        }
    }
}