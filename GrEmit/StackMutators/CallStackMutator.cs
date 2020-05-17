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
            if (parameter is MethodILInstructionParameter)
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
            for (var i = parameterTypes.Length - 1; i >= 0; --i)
            {
                CheckNotEmpty(il, stack, () => $"Parameter #{i + 1} for call to the method '{formattedMethodGetter()}' is not loaded on the evaluation stack");
                CheckCanBeAssigned(il, parameterTypes[i], stack.Pop());
            }
            if (!isStatic)
            {
                CheckNotEmpty(il, stack, () => $"An instance to call the method '{formattedMethodGetter()}' is not loaded on the evaluation stack");
                var instance = stack.Pop();
                var instanceBaseType = instance.ToType();
                if (instanceBaseType != null)
                {
                    if (!instanceBaseType.IsByRef)
                        CheckCanBeAssigned(il, declaringType, instance);
                    else
                    {
                        var elementType = instanceBaseType.GetElementType();
                        if (!elementType.IsValueType)
                            ThrowError(il, $"Cannot call the method '{formattedMethodGetter()}' on an instance of type '{instance}'");
                        else
                        {
                            if (declaringType.IsInterface)
                            {
                                if (ReflectionExtensions.GetInterfaces(elementType).All(type => type != declaringType))
                                    ThrowError(il, $"The type '{Formatter.Format(elementType)}' does not implement interface '{Formatter.Format(declaringType)}'");
                            }
                            else if (declaringType != typeof(object) && declaringType != elementType)
                                ThrowError(il, $"Cannot call the method '{formattedMethodGetter()}' on an instance of type '{elementType}'");
                            if (isVirtual && callvirt)
                            {
                                if (constrained == null)
                                    ThrowError(il, $"In order to call a virtual method '{formattedMethodGetter()}' on a value type '{Formatter.Format(elementType)}' specify 'constrained' parameter");
                                if (constrained != elementType)
                                    ThrowError(il, $"Invalid 'constrained' parameter to call a virtual method '{formattedMethodGetter()}'. Expected '{Formatter.Format(constrained)}' but was '{Formatter.Format(elementType)}'");
                            }
                        }
                    }
                }
            }
            if (returnType != typeof(void))
                stack.Push(returnType);
        }

        private readonly bool callvirt;
    }
}
