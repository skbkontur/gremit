using GrEmit.InstructionParameters;
using GrEmit.Utils;

namespace GrEmit.StackMutators
{
    internal class LdfldStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            var field = ((FieldILInstructionParameter)parameter).Field;
            if (!field.IsStatic)
            {
                var declaringType = field.DeclaringType;
                CheckNotEmpty(il, stack, () => $"In order to load the field '{Formatter.Format(field)}' an instance must be put onto the evaluation stack");

                var instance = stack.Pop().ToType();
                if (instance != null)
                {
                    if (!instance.IsByRef)
                        CheckCanBeAssigned(il, declaringType, instance);
                    else
                    {
                        var elementType = instance.GetElementType();
                        if (elementType.IsValueType)
                        {
                            if (declaringType != elementType)
                                ThrowError(il, $"Cannot load the field '{Formatter.Format(field)}' of an instance of type '{Formatter.Format(elementType)}'");
                        }
                        else
                            ThrowError(il, $"Cannot load the field '{Formatter.Format(field)}' of an instance of type '{Formatter.Format(instance)}'");
                    }
                }
            }
            stack.Push(field.FieldType);
        }
    }
}
