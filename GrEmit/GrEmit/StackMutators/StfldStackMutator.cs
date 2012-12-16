using System;
using System.Collections.Generic;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class StfldStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            var field = ((FieldILInstructionParameter)parameter).Field;
            CheckNotEmpty(stack);
            CheckCanBeAssigned(field.FieldType, stack.Pop());
            CheckNotEmpty(stack);
            CheckNotStruct(stack.Pop());
        }
    }
}