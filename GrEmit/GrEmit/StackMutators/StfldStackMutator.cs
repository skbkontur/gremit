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
            CheckNotEmpty(il, stack);
            CheckCanBeAssigned(il, field.FieldType, stack.Pop());
            if (!field.IsStatic)
            {
                CheckNotEmpty(il, stack);
                CheckNotStruct(il, stack.Pop());
            }
        }
    }
}