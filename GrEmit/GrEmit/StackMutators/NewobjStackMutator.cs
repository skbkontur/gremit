using System;
using System.Collections.Generic;
using System.Reflection;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class NewobjStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            var constructor = ((ConstructorILInstructionParameter)parameter).Constructor;
            ParameterInfo[] parameterInfos = constructor.GetParameters();
            for(int i = parameterInfos.Length - 1; i >= 0; --i)
            {
                CheckNotEmpty(il, stack);
                CheckCanBeAssigned(il, parameterInfos[i].ParameterType, stack.Pop());
            }
            stack.Push(constructor.ReflectedType);
        }
    }
}