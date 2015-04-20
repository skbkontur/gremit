using System;
using System.Linq;

using GrEmit.InstructionParameters;
using GrEmit.Utils;

namespace GrEmit.StackMutators
{
    internal class CallStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
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
                parameterTypes = ReflectionExtensions.GetParameterTypes(method);
                returnType = ReflectionExtensions.GetReturnType(method);
                isStatic = method.IsStatic;
                formattedMethod = Formatter.Format(method);
            }
            else
            {
                var constructor = ((ConstructorILInstructionParameter)parameter).Constructor;
                declaringType = constructor.DeclaringType;
                parameterTypes = ReflectionExtensions.GetParameterTypes(constructor);
                returnType = typeof(void);
                isStatic = false;
                formattedMethod = Formatter.Format(constructor);
            }
            for(var i = parameterTypes.Length - 1; i >= 0; --i)
            {
                CheckNotEmpty(il, stack, string.Format("Parameter #{0} for call to method '{1}' is not loaded on the evaluation stack", i + 1, formattedMethod));
                CheckCanBeAssigned(il, parameterTypes[i], stack.Pop());
            }
            if(!isStatic)
            {
                CheckNotEmpty(il, stack, string.Format("An intance to call method '{0}' is not loaded on the evaluation stack", formattedMethod));
                var instance = stack.Pop();
                var instanceBaseType = instance.ToType();
                if(instanceBaseType != null)
                {
                    if(instanceBaseType.IsValueType)
                        ThrowError(il, string.Format("In order to call method '{0}' on a value type '{1}' load instance by ref or box it", formattedMethod, instance));
                    else if(!instanceBaseType.IsByRef)
                        CheckCanBeAssigned(il, declaringType, instance);
                    else
                    {
                        var elementType = instanceBaseType.GetElementType();
                        if(elementType.IsValueType)
                        {
                            if(declaringType.IsInterface)
                            {
                                if(ReflectionExtensions.GetInterfaces(elementType).All(type => type != declaringType))
                                    ThrowError(il, string.Format("Type '{0}' does not implement interface '{1}'", Formatter.Format(elementType), Formatter.Format(declaringType)));
                            }
                            else if(declaringType != typeof(object) && declaringType != elementType)
                                ThrowError(il, string.Format("Cannot call method '{0}' on type '{1}'", formattedMethod, elementType));
                        }
                        else
                            ThrowError(il, string.Format("Cannot call method '{0}' on type '{1}'", formattedMethod, instance));
                    }
                }
            }
            if(returnType != typeof(void))
                stack.Push(returnType);
        }
    }
}