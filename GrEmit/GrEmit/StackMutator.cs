using System;
using System.Collections.Generic;
using System.Linq;

using GrEmit.InstructionComments;

namespace GrEmit
{
    internal abstract class StackMutator
    {
        public abstract void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack);

        protected static bool IsAddressType(Type type)
        {
            return type.IsByRef || type.IsPointer || type == typeof(IntPtr) || type == (IntPtr.Size == 4 ? typeof(int) : typeof(long));
        }

        protected static void CheckNotStruct(Type type)
        {
            if(IsStruct(type))
                throw new InvalidOperationException("Struct of type '" + type + "' is not valid at this point");
        }

        protected void CheckNotEmpty(Stack<Type> stack)
        {
            if(stack.Count == 0)
                throw new InvalidOperationException("Stack is empty");
        }

        protected static bool StacksConsistent(Stack<Type> stack, Type[] otherStack)
        {
            if(otherStack.Length != stack.Count)
                return false;
            var currentStack = stack.Reverse().ToArray();
            for(int i = 0; i < otherStack.Length; ++i)
            {
                Type type1 = currentStack[i];
                Type type2 = otherStack[i];
                if(!type1.IsValueType && !type2.IsValueType)
                    continue;
                if(IsStruct(type1) || IsStruct(type2))
                {
                    if(type1 != type2)
                        return false;
                }
                else if(GetSize(type1) != GetSize(type2))
                    return false;
            }
            return true;
        }

        protected static bool IsStruct(Type type)
        {
            return type.IsValueType && !type.IsPrimitive && !type.IsEnum;
        }

        protected static void CheckIsAddress(Type peek)
        {
            if(!IsAddressType(peek))
                throw new InvalidOperationException("An address type expected but was '" + peek + "'");
        }

        protected static void CheckCanBeAssigned(Type to, Type from)
        {
            if(!CanBeAssigned(to, from))
                throw new InvalidOperationException("Unable to set value of type '" + from + "' to value of type '" + to + "'");
        }

        protected static bool CanBeAssigned(Type to, Type from)
        {
            if(!to.IsValueType && !from.IsValueType)
                return true;
            if(!to.IsValueType && from.IsValueType)
                return false;
            if(IsStruct(to) || IsStruct(from))
                return to == from;
            return GetSize(to) == GetSize(from);
        }

        protected static int GetSize(Type type)
        {
            if(type == typeof(IntPtr))
                return IntPtr.Size;
            switch(Type.GetTypeCode(type))
            {
            case TypeCode.Boolean:
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.Char:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.Single:
                return 4;
            case TypeCode.Int64:
            case TypeCode.UInt64:
            case TypeCode.Double:
                return 8;
            case TypeCode.Object:
                return IntPtr.Size;
            default:
                throw new NotSupportedException("Type '" + type + "' is not supported");
            }
        }

        protected void CheckStacksEqual(GroboIL.Label label, Stack<Type> stack, Type[] otherStack)
        {
            if(!StacksConsistent(stack, otherStack))
                throw new InvalidOperationException("Incosistent stack for label '" + label.Name + "'");
        }

        protected void SaveOrCheck(GroboIL il, Stack<Type> stack, GroboIL.Label label)
        {
            Type[] labelStack;
            if(il.labelStacks.TryGetValue(label, out labelStack))
                CheckStacksEqual(label, stack, labelStack);
            else
            {
                int lineNumber = il.ilCode.GetLabelLineNumber(label);
                Type[] array = stack.Reverse().ToArray();
                il.labelStacks.Add(label, array);
                if(lineNumber >= 0)
                {
                    var curStack = new Stack<Type>(array);
                    while(true)
                    {
                        var comment = il.ilCode.GetComment(lineNumber);
                        if(comment == null)
                            break;
                        ILCode.ILInstruction instruction = il.ilCode.GetInstruction(lineNumber);
                        StackMutatorCollection.Mutate(instruction.OpCode, il, instruction.Parameter, ref curStack);
                        if (comment is StackILInstructionComment)
                        {
                            var instructionStack = ((StackILInstructionComment)comment).Stack;
                            if(!StacksConsistent(curStack, instructionStack))
                                throw new InvalidOperationException("Inconsistent stacks for line " + (lineNumber + 1));
                        }
                        else
                            il.ilCode.SetComment(lineNumber, new StackILInstructionComment(curStack.Reverse().ToArray()));
                        ++lineNumber;
                    }
                }
            }
        }
    }
}