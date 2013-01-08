using System;
using System.Collections.Generic;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class LdfldStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            var field = ((FieldILInstructionParameter)parameter).Field;
            if (!field.IsStatic)
            {
                CheckNotEmpty(il, stack);
                CheckNotStruct(il, stack.Pop());
            }
            stack.Push(field.FieldType);
        }
    }
}