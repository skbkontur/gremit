using System;
using System.Collections.Generic;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class LdfldaStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            var field = ((FieldILInstructionParameter)parameter).Field;
            CheckNotEmpty(stack);
            CheckNotStruct(stack.Pop());
            stack.Push(field.FieldType.MakeByRefType());
        }
    }
}