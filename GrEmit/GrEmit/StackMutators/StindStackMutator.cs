using System;
using System.Collections.Generic;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class StindStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            var type = ((TypeILInstructionParameter)parameter).Type;
            CheckNotEmpty(il, stack);
            CheckCanBeAssigned(il, type, stack.Pop());
            CheckNotEmpty(il, stack);
            Type pointer = stack.Pop();
            CheckIsAPointer(il, pointer);
            if(pointer.IsByRef || pointer.IsPointer)
            {
                var elementType = pointer.GetElementType();
                if(elementType.IsValueType)
                    CheckCanBeAssigned(il, pointer, type.MakeByRefType());
                else
                    CheckCanBeAssigned(il, elementType, type);
            }
            else if(pointer.IsPointer)
            {
                var elementType = pointer.GetElementType();
                if(elementType.IsValueType)
                    CheckCanBeAssigned(il, pointer, type.MakePointerType());
                else
                    CheckCanBeAssigned(il, elementType, type);
            }
        }
    }
}