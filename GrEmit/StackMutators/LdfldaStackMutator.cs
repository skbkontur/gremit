using GrEmit.InstructionParameters;
using GrEmit.Utils;

namespace GrEmit.StackMutators
{
    internal class LdfldaStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            var field = ((FieldILInstructionParameter)parameter).Field;
            if(!field.IsStatic)
            {
                var declaringType = field.DeclaringType;
                CheckNotEmpty(il, stack, () => string.Format("In order to load a reference to the field '{0}' an instance must be put onto the evaluation stack", Formatter.Format(field)));

                var instance = stack.Pop().ToType();
                if(instance != null)
                {
                    if(instance.IsValueType)
                        ThrowError(il, string.Format("In order to load a reference to the field '{0}' of a value type '{1}' load an instance by ref", Formatter.Format(field), Formatter.Format(instance)));
                    else if(!instance.IsByRef)
                        CheckCanBeAssigned(il, declaringType, instance);
                    else
                    {
                        var elementType = instance.GetElementType();
                        if(elementType.IsValueType)
                        {
                            if(declaringType != elementType)
                                ThrowError(il, string.Format("Cannot load a reference to the field '{0}' of an instance of type '{1}'", Formatter.Format(field), Formatter.Format(elementType)));
                        }
                        else
                            ThrowError(il, string.Format("Cannot load a reference to the field '{0}' of an instance of type '{1}'", Formatter.Format(field), Formatter.Format(instance)));
                    }
                }
            }
            stack.Push(field.FieldType.MakeByRefType());
        }
    }
}