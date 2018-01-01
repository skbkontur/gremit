using GrEmit.InstructionParameters;
using GrEmit.Utils;

namespace GrEmit.StackMutators
{
    internal class StfldStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            var field = ((FieldILInstructionParameter)parameter).Field;
            CheckNotEmpty(il, stack, () => string.Format("In order to store the field '{0}' a value must be put onto the evaluation stack", Formatter.Format(field)));
            CheckCanBeAssigned(il, field.FieldType, stack.Pop());
            if(!field.IsStatic)
            {
                var declaringType = field.DeclaringType;
                CheckNotEmpty(il, stack, () => string.Format("In order to store the field '{0}' an instance must be put onto the evaluation stack", Formatter.Format(field)));

                var instance = stack.Pop().ToType();
                if(instance != null)
                {
                    if(instance.IsValueType)
                        ThrowError(il, string.Format("In order to store the field '{0}' of a value type '{1}' load an instance by ref", Formatter.Format(field), Formatter.Format(instance)));
                    else if(!instance.IsByRef)
                        CheckCanBeAssigned(il, declaringType, instance);
                    else
                    {
                        var elementType = instance.GetElementType();
                        if(elementType.IsValueType)
                        {
                            if(declaringType != elementType)
                                ThrowError(il, string.Format("Cannot store the field '{0}' to an instance of type '{1}'", Formatter.Format(field), Formatter.Format(elementType)));
                        }
                        else
                            ThrowError(il, string.Format("Cannot store the field '{0}' to an instance of type '{1}'", Formatter.Format(field), Formatter.Format(instance)));
                    }
                }
            }
        }
    }
}