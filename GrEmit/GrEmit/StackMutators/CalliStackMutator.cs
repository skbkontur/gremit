using System;
using System.Collections.Generic;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class CalliStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            var calliParameter = (MethodByAddressILInstructionParameter)parameter;
            var returnType = calliParameter.ReturnType;
            var parameterTypes = calliParameter.ParameterTypes;
            CheckNotEmpty(stack);
            CheckIsAddress(stack.Pop());
            for (int i = parameterTypes.Length - 1; i >= 0; --i)
            {
                CheckNotEmpty(stack);
                CheckCanBeAssigned(parameterTypes[i], stack.Pop());
            }
            if (returnType != typeof(void))
                stack.Push(returnType);
        }
    }
}