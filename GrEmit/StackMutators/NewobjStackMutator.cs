using GrEmit.InstructionParameters;
using GrEmit.Utils;

namespace GrEmit.StackMutators
{
    internal class NewobjStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            var constructor = ((ConstructorILInstructionParameter)parameter).Constructor;
            var parameterTypes = ReflectionExtensions.GetParameterTypes(constructor);
            for(var i = parameterTypes.Length - 1; i >= 0; --i)
            {
                CheckNotEmpty(il, stack, () => string.Format("Expected exactly {0} parameters to call the constructor '{1}'", parameterTypes.Length, Formatter.Format(constructor)));
                CheckCanBeAssigned(il, parameterTypes[i], stack.Pop());
            }
            stack.Push(constructor.ReflectedType);
        }
    }
}