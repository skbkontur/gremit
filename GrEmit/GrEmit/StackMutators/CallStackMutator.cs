using System;
using System.Collections.Generic;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class CallStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            Type[] parameterTypes;
            Type returnType;
            bool isStatic;
            if(parameter is MethodILInstructionParameter)
            {
                var method = ((MethodILInstructionParameter)parameter).Method;
                parameterTypes = Formatter.GetParameterTypes(method);
                returnType = method.ReturnType;
                isStatic = method.IsStatic;
            }
            else
            {
                var constructor = ((ConstructorILInstructionParameter)parameter).Constructor;
                parameterTypes = Formatter.GetParameterTypes(constructor);
                returnType = typeof(void);
                isStatic = false;
            }
            for(int i = parameterTypes.Length - 1; i >= 0; --i)
            {
                CheckNotEmpty(il, stack);
                CheckCanBeAssigned(il, parameterTypes[i], stack.Pop());
            }
            if(!isStatic)
            {
                CheckNotEmpty(il, stack);
                CheckNotStruct(il, stack.Pop());
            }
            if(returnType != typeof(void))
                stack.Push(returnType);
        }
    }
}