using System;
using System.Collections.Generic;
using System.Linq;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class CallStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            Type[] parameterTypes;
            Type returnType;
            Type declaringType;
            bool isStatic;
            string formattedMethod;
            if(parameter is MethodILInstructionParameter)
            {
                var method = ((MethodILInstructionParameter)parameter).Method;
                declaringType = method.DeclaringType;
                parameterTypes = Formatter.GetParameterTypes(method);
                returnType = method.ReturnType;
                isStatic = method.IsStatic;
                formattedMethod = Formatter.Format(method);
            }
            else
            {
                var constructor = ((ConstructorILInstructionParameter)parameter).Constructor;
                declaringType = constructor.DeclaringType;
                parameterTypes = Formatter.GetParameterTypes(constructor);
                returnType = typeof(void);
                isStatic = false;
                formattedMethod = Formatter.Format(constructor);
            }
            for(var i = parameterTypes.Length - 1; i >= 0; --i)
            {
                CheckNotEmpty(il, stack);
                CheckCanBeAssigned(il, parameterTypes[i], stack.Pop());
            }
            if(!isStatic)
            {
                CheckNotEmpty(il, stack);
                var instance = stack.Pop();
                if(instance.IsValueType)
                    ThrowError(il, string.Format("In order to call method '{0}' on a value type '{1}' load instance by ref or box it", formattedMethod, Formatter.Format(instance)));
                else if(instance.IsByRef)
                {
                    var elementType = instance.GetElementType();
                    if(elementType.IsValueType)
                    {
                        if(declaringType.IsInterface)
                        {
                            if(elementType.GetInterfaces().All(type => type != declaringType))
                                ThrowError(il, string.Format("Type '{0}' does not implement interface '{1}'", Formatter.Format(elementType), Formatter.Format(declaringType)));
                        }
                        else if(declaringType != typeof(object) && declaringType != elementType)
                            ThrowError(il, string.Format("Cannot call method '{0}' on type '{1}'", formattedMethod, elementType));
                    }
                    else
                        ThrowError(il, string.Format("Cannot call method '{0}' on type '{1}'", formattedMethod, instance));
                }
                else CheckCanBeAssigned(il, declaringType, instance);
            }
            if(returnType != typeof(void))
                stack.Push(returnType);
        }
    }
}