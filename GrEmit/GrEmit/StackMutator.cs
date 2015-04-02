using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using GrEmit.InstructionComments;

namespace GrEmit
{
    internal enum CLIType
    {
        Zero,
        Int32,
        Int64,
        NativeInt,
        Float,
        Object,
        Pointer,
        Struct
    }

    internal abstract class StackMutator
    {
        public abstract void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack);

        protected static void ThrowError(GroboIL il, string message)
        {
            throw new InvalidOperationException(message + Environment.NewLine + il.GetILCode());
        }

        protected static void CheckNotStruct(GroboIL il, Type type)
        {
            if(ToCLIType(type) == CLIType.Struct)
                ThrowError(il, string.Format("Struct of type '{0}' is not valid at this point", type));
        }

        protected void CheckNotEmpty(GroboIL il, Stack<Type> stack)
        {
            if(stack.Count == 0)
                throw new InvalidOperationException("Stack is empty\r\n" + il.GetILCode());
        }

        protected static bool TypesConsistent(Type type1, Type type2)
        {
            var ilStackType1 = ToCLIType(type1);
            var ilStackType2 = ToCLIType(type2);
            if(ilStackType1 != ilStackType2)
                return false;
            if(ilStackType1 == CLIType.Struct && type1 != type2)
                return false;
            return true;
        }

        protected static bool StacksConsistent(Stack<Type> stack, Type[] otherStack)
        {
            if(otherStack.Length != stack.Count)
                return false;
            var currentStack = stack.Reverse().ToArray();
            for(var i = 0; i < otherStack.Length; ++i)
            {
                var type1 = currentStack[i];
                var type2 = otherStack[i];
                if(!TypesConsistent(type1, type2))
                    return false;
/*                if(!type1.IsValueType && !type2.IsValueType)
                    continue;
                if(IsStruct(type1) || IsStruct(type2))
                {
                    if(type1 != type2)
                        return false;
                }
                else if(GetSize(type1) != GetSize(type2))
                    return false;*/
            }
            return true;
        }

        protected static void CheckIsAPointer(GroboIL il, Type type)
        {
            var cliType = ToCLIType(type);
            if(cliType != CLIType.Pointer && cliType != CLIType.NativeInt)
                ThrowError(il, string.Format("A pointer type expected but was '{0}'", type));
        }

        protected static void CheckCanBeAssigned(GroboIL il, Type to, Type from)
        {
            if(!CanBeAssigned(to, from))
                ThrowError(il, string.Format("Unable to set value of type '{0}' to value of type '{1}'", from, to));
        }

        protected static bool CanBeAssigned(Type to, Type from)
        {
            var cliTo = ToCLIType(to);
            var cliFrom = ToCLIType(from);
            switch(cliTo)
            {
            case CLIType.Int32:
                return cliFrom == CLIType.Int32 || cliFrom == CLIType.NativeInt || cliFrom == CLIType.Zero;
            case CLIType.NativeInt:
                return cliFrom == CLIType.Int32 || cliFrom == CLIType.NativeInt || cliFrom == CLIType.Zero;
            case CLIType.Int64:
                return cliFrom == CLIType.Int64 || cliFrom == CLIType.Zero;
            case CLIType.Float:
                return cliFrom == CLIType.Float || cliFrom == CLIType.Zero;
            case CLIType.Struct:
                return from == to;
            case CLIType.Pointer:
                if(cliFrom == CLIType.Zero || from == to)
                    return true;
                if(cliFrom != CLIType.Pointer)
                    return false;
                to = to.GetElementType();
                from = from.GetElementType();
                return to.IsValueType && from.IsValueType;
            case CLIType.Object:
                if(cliFrom == CLIType.Zero)
                    return true;
                if(to.IsGenericParameter)
                    return to == from;
                return to.IsAssignableFrom(from);
            default:
                throw new InvalidOperationException(string.Format("CLI type '{0}' is not valid at this point", cliTo));
            }
        }

        protected void CheckStacksEqual(GroboIL il, GroboIL.Label label, Stack<Type> stack, Type[] otherStack)
        {
            if(!StacksConsistent(stack, otherStack))
                throw new InvalidOperationException("Inconsistent stack for label '" + label.Name + "'\r\n" + il.GetILCode());
        }

        protected void SaveOrCheck(GroboIL il, Stack<Type> stack, GroboIL.Label label)
        {
            Type[] labelStack;
            if(il.labelStacks.TryGetValue(label, out labelStack))
            {
                Type[] merged;
                var comparisonResult = CompareStacks(stack.Reverse().ToArray(), labelStack, out merged);
                switch(comparisonResult)
                {
                case StacksComparisonResult.Equal:
                    return;
                case StacksComparisonResult.Inconsistent:
                    ThrowError(il, string.Format("Inconsistent stack for label '{0}'", label.Name));
                    break;
                case StacksComparisonResult.Equivalent:
                    il.labelStacks[label] = merged;
                    Propogate(il, il.ilCode.GetLabelLineNumber(label), new Stack<Type>(merged));
                    break;
                }
            }
            else
            {
                il.labelStacks.Add(label, stack.Reverse().ToArray());
                Propogate(il, il.ilCode.GetLabelLineNumber(label), stack);
            }
        }

        protected static Type Canonize(Type type)
        {
            if(type == null) return null;
            switch(Type.GetTypeCode(type))
            {
            case TypeCode.Boolean:
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Char:
                type = typeof(int);
                break;
            case TypeCode.Int64:
            case TypeCode.UInt64:
                type = typeof(long);
                break;
            }
            return type;
        }

        protected static CLIType ToCLIType(Type type)
        {
            if(type == null)
                return CLIType.Zero;
            if(!type.IsValueType)
            {
                if(type.IsByRef)
                    return CLIType.Pointer;
                return type.IsPointer ? CLIType.NativeInt : CLIType.Object;
            }
            if(type.IsPrimitive)
            {
                if(type == typeof(IntPtr) || type == typeof(UIntPtr))
                    return CLIType.NativeInt;
                var typeCode = Type.GetTypeCode(type);
                switch(typeCode)
                {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.Char:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                    return CLIType.Int32;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return CLIType.Int64;
                case TypeCode.Double:
                case TypeCode.Single:
                    return CLIType.Float;
                default:
                    return CLIType.Struct;
                }
            }
            return type.IsEnum ? ToCLIType(Enum.GetUnderlyingType(type)) : CLIType.Struct;
        }

        private void Propogate(GroboIL il, int lineNumber, Stack<Type> stack)
        {
            if(lineNumber < 0)
                return;
            while(true)
            {
                var comment = il.ilCode.GetComment(lineNumber);
                if(comment == null)
                    break;
                var instruction = (ILCode.ILInstruction)il.ilCode.GetInstruction(lineNumber);
                StackMutatorCollection.Mutate(instruction.OpCode, il, instruction.Parameter, ref stack);
                Type[] merged;
                if(comment is StackILInstructionComment)
                {
                    var instructionStack = ((StackILInstructionComment)comment).Stack;

                    var comparisonResult = CompareStacks(stack.Reverse().ToArray(), instructionStack, out merged);
                    switch(comparisonResult)
                    {
                    case StacksComparisonResult.Equal:
                        return;
                    case StacksComparisonResult.Inconsistent:
                        ThrowError(il, string.Format("Inconsistent stacks for line {0}", (lineNumber + 1)));
                        break;
                    case StacksComparisonResult.Equivalent:
                        stack = new Stack<Type>(merged);
                        break;
                    }
                }
                il.ilCode.SetComment(lineNumber, new StackILInstructionComment(stack.Reverse().ToArray()));
                ++lineNumber;
            }
        }

        private StacksComparisonResult CompareStacks(Type[] first, Type[] second, out Type[] merged)
        {
            merged = null;
            if(first.Length != second.Length)
                return StacksComparisonResult.Inconsistent;
            Type[] result = null;
            for(var i = 0; i < first.Length; ++i)
            {
                var firstCLIType = ToCLIType(first[i]);
                var secondCLIType = ToCLIType(second[i]);
                if(firstCLIType != CLIType.Zero && secondCLIType != CLIType.Zero && firstCLIType != secondCLIType)
                    return StacksComparisonResult.Inconsistent;
                if(first[i] != second[i])
                {
                    var common = FindCommonType(firstCLIType, first[i], second[i]);
                    if(common == null)
                        return StacksComparisonResult.Inconsistent;
                    if(result == null)
                    {
                        result = new Type[first.Length];
                        for(var j = 0; j < i; ++j)
                            result[j] = first[j];
                    }
                    result[i] = common;
                }
                else if(result != null)
                    result[i] = first[i];
            }
            if(result == null)
                return StacksComparisonResult.Equal;
            merged = result;
            return StacksComparisonResult.Equivalent;
        }

        private static Type FindCommonType(CLIType cliType, Type first, Type second)
        {
            if(first == null) return second;
            if(second == null) return first;
            switch(cliType)
            {
            case CLIType.Int32:
                return typeof(int);
            case CLIType.Int64:
                return typeof(long);
            case CLIType.Float:
                return typeof(double);
            case CLIType.NativeInt:
                {
                    if(first.IsPointer && second.IsPointer)
                        return typeof(void).MakePointerType();
                    return typeof(IntPtr);
                }
            case CLIType.Pointer:
                {
                    var firstElementType = first.GetElementType();
                    var secondElementType = second.GetElementType();
                    if(!firstElementType.IsValueType || !secondElementType.IsValueType)
                        return null;
                    return Marshal.SizeOf(firstElementType) <= Marshal.SizeOf(secondElementType) ? first : second;
                }
            case CLIType.Struct:
                return null;
            case CLIType.Object:
                return first.FindEqualTypeWith(second);
            default:
                throw new InvalidOperationException(string.Format("CLI type '{0}' is not valid at this point", cliType));
            }
        }

        private enum StacksComparisonResult
        {
            Inconsistent,
            Equal,
            Equivalent,
        }
    }
}