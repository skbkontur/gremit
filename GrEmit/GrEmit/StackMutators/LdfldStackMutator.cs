using GrEmit.InstructionParameters;
using GrEmit.Utils;

namespace GrEmit.StackMutators
{
    internal class LdfldStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            var field = ((FieldILInstructionParameter)parameter).Field;
            if(!field.IsStatic)
            {
                var declaringType = field.DeclaringType;
                CheckNotEmpty(il, stack);

                var instance = stack.Pop().ToType();
                if(instance != null)
                {
                    if(instance.IsValueType)
                        ThrowError(il, string.Format("In order to load field '{0}' of a value type '{1}' load instance by ref", field, Formatter.Format(instance)));
                    else if(!instance.IsByRef)
                        CheckCanBeAssigned(il, declaringType, instance);
                    else
                    {
                        var elementType = instance.GetElementType();
                        if(elementType.IsValueType)
                        {
                            if(declaringType != elementType)
                                ThrowError(il, string.Format("Cannot load field '{0}' of type '{1}'", field, elementType));
                        }
                        else
                            ThrowError(il, string.Format("Cannot load field '{0}' of type '{1}'", field, instance));
                    }
                }
            }
            stack.Push(field.FieldType);
        }
    }
}