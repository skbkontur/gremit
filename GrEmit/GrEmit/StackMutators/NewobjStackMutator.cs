using System;
using System.Collections.Generic;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class NewobjStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            var constructor = ((ConstructorILInstructionParameter)parameter).Constructor;
            var parameterTypes = Formatter.GetParameterTypes(constructor);
            for(int i = parameterTypes.Length - 1; i >= 0; --i)
            {
                CheckNotEmpty(il, stack);
                CheckCanBeAssigned(il, parameterTypes[i], stack.Pop());
            }
            stack.Push(constructor.ReflectedType);
        }
    }
}