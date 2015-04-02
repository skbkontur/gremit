using System;
using System.Collections.Generic;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class LdindStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            var type = ((TypeILInstructionParameter)parameter).Type;
            CheckNotEmpty(il, stack);
            var pointer = stack.Pop();
            CheckIsAPointer(il, pointer);
            if(pointer.IsByRef)
            {
                var elementType = pointer.GetElementType();
                if(elementType.IsValueType)
                    CheckCanBeAssigned(il, type.MakeByRefType(), pointer);
                else
                    CheckCanBeAssigned(il, type, elementType);
            }
            else if(pointer.IsPointer)
            {
                var elementType = pointer.GetElementType();
                if(elementType.IsValueType)
                    CheckCanBeAssigned(il, type.MakePointerType(), pointer);
                else
                    CheckCanBeAssigned(il, type, elementType);
            }
            else if(!type.IsPrimitive && type != typeof(object))
                ThrowError(il, string.Format("Unable to load a value of type '{0}' from pointer of type '{1}' indirectly", Formatter.Format(type), Formatter.Format(pointer)));
            stack.Push(type);
        }
    }
}