using System;
using System.Linq;

using GrEmit.InstructionParameters;
using GrEmit.Utils;

namespace GrEmit.StackMutators
{
    internal class CallStackMutator : StackMutator
    {
        public CallStackMutator(bool callvirt)
        {
            this.callvirt = callvirt;
        }

        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            Type[] parameterTypes;
            Type returnType;
            Type declaringType;
            Type constrained;
            bool isStatic;
            bool isVirtual;
            Func<string> formattedMethodGetter;
            if(parameter is MethodILInstructionParameter)
            {
                var method = ((MethodILInstructionParameter)parameter).Method;
                declaringType = method.DeclaringType;
                parameterTypes = ReflectionExtensions.GetParameterTypes(method);
                returnType = ReflectionExtensions.GetReturnType(method);
                var callILInstructionParameter = parameter as CallILInstructionParameter;
                constrained = callILInstructionParameter == null ? null : callILInstructionParameter.Constrained;
                isStatic = method.IsStatic;
                isVirtual = method.IsVirtual;
                formattedMethodGetter = () => Formatter.Format(method);
            }
            else
            {
                var constructor = ((ConstructorILInstructionParameter)parameter).Constructor;
                declaringType = constructor.DeclaringType;
                parameterTypes = ReflectionExtensions.GetParameterTypes(constructor);
                returnType = typeof(void);
                constrained = null;
                isStatic = false;
                isVirtual = false;
                formattedMethodGetter = () => Formatter.Format(constructor);
            }
            for(var i = parameterTypes.Length - 1; i >= 0; --i)
            {
                CheckNotEmpty(il, stack, () => string.Format("Parameter #{0} for call to the method '{1}' is not loaded on the evaluation stack", i + 1, formattedMethodGetter()));
                CheckCanBeAssigned(il, parameterTypes[i], stack.Pop());
            }
            if(!isStatic)
            {
                CheckNotEmpty(il, stack, () => string.Format("An instance to call the method '{0}' is not loaded on the evaluation stack", formattedMethodGetter()));
                var instance = stack.Pop();
                var instanceBaseType = instance.ToType();
                if(instanceBaseType != null)
                {
                    if(instanceBaseType.IsValueType)
                        ThrowError(il, string.Format("In order to call the method '{0}' on a value type '{1}' load an instance by ref or box it", formattedMethodGetter(), instance));
                    else if(!instanceBaseType.IsByRef)
                        CheckCanBeAssigned(il, declaringType, instance);
                    else
                    {
                        var elementType = instanceBaseType.GetElementType();
                        if(!elementType.IsValueType)
                            ThrowError(il, string.Format("Cannot call the method '{0}' on an instance of type '{1}'", formattedMethodGetter(), instance));
                        else
                        {
                            if(declaringType.IsInterface)
                            {
                                if(ReflectionExtensions.GetInterfaces(elementType).All(type => type != declaringType))
                                    ThrowError(il, string.Format("The type '{0}' does not implement interface '{1}'", Formatter.Format(elementType), Formatter.Format(declaringType)));
                            }
                            else if(declaringType != typeof(object) && declaringType != elementType)
                                ThrowError(il, string.Format("Cannot call the method '{0}' on an instance of type '{1}'", formattedMethodGetter(), elementType));
                            if(isVirtual && callvirt)
                            {
                                if(constrained == null)
                                    ThrowError(il, string.Format("In order to call a virtual method '{0}' on a value type '{1}' specify 'constrained' parameter", formattedMethodGetter(), Formatter.Format(elementType)));
                                if(constrained != elementType)
                                    ThrowError(il, string.Format("Invalid 'constrained' parameter to call a virtual method '{0}'. Expected '{1}' but was '{2}'", formattedMethodGetter(), Formatter.Format(constrained), Formatter.Format(elementType)));
                            }
                        }
                    }
                }
            }
            if(returnType != typeof(void))
                stack.Push(returnType);
        }

        private readonly bool callvirt;
    }
}