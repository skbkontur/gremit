using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

using GrEmit.InstructionComments;
using GrEmit.Utils;

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

    internal abstract class ESType
    {
        public abstract Type ToType();

        public override string ToString()
        {
            return Formatter.Format(this);
        }

        public static readonly ESType Zero = new SimpleESType(null);
    }

    internal class SimpleESType : ESType
    {
        public SimpleESType(Type type)
        {
            Type = type;
        }

        public override Type ToType()
        {
            return Type;
        }

        public Type Type { get; private set; }
    }

    internal class ComplexESType : ESType
    {
        public ComplexESType(Type baseType, Type[] interfaces)
        {
            BaseType = baseType;
            Interfaces = interfaces;
        }

        public override Type ToType()
        {
            return BaseType;
        }

        public Type BaseType { get; private set; }
        public Type[] Interfaces { get; private set; }
    }

    internal class EvaluationStack : Stack<ESType>
    {
        public EvaluationStack()
        {
        }

        public EvaluationStack(IEnumerable<ESType> collection)
            : base(collection)
        {
        }

        public void Push(Type type)
        {
            if(type == null)
                Push(ESType.Zero);
            else if(type.IsInterface && !type.IsByRef && !type.IsPointer) // todo hack for Mono: typeof(IList).MakeByRefType().IsInterface = true
                Push(new ComplexESType(typeof(object), new[] {type}));
            else
                Push(new SimpleESType(type));
        }

        public override string ToString()
        {
            return new StackILInstructionComment(this.Reverse().ToArray()).Format();
        }
    }

    internal abstract class StackMutator
    {
        public abstract void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack);

        protected static void ThrowError(GroboIL il, string message)
        {
            throw new InvalidOperationException(message + Environment.NewLine + il.GetILCode());
        }

        protected static void CheckNotStruct(GroboIL il, ESType type)
        {
            if(ToCLIType(type) == CLIType.Struct)
                ThrowError(il, string.Format("Struct of type '{0}' is not valid at this point", type));
        }

        protected static void CheckNotEmpty(GroboIL il, EvaluationStack stack, Func<string> message)
        {
            if(stack.Count == 0)
                ThrowError(il, message());
        }

        protected static void CheckIsAPointer(GroboIL il, ESType type)
        {
            var cliType = ToCLIType(type);
            if(cliType != CLIType.Pointer && cliType != CLIType.NativeInt)
                ThrowError(il, string.Format("A pointer type expected but was '{0}'", type));
        }

        protected static void CheckCanBeAssigned(GroboIL il, Type to, ESType from)
        {
            if(!CanBeAssigned(to, from, il.VerificationKind))
                ThrowError(il, string.Format("Unable to set a value of type '{0}' to an instance of type '{1}'", from, Formatter.Format(to)));
        }

        protected static void CheckCanBeAssigned(GroboIL il, Type to, Type from)
        {
            if(!CanBeAssigned(to, from, il.VerificationKind))
                ThrowError(il, string.Format("Unable to set a value of type '{0}' to an instance of type '{1}'", Formatter.Format(from), Formatter.Format(to)));
        }

        protected static bool CanBeAssigned(Type to, Type from, TypesAssignabilityVerificationKind verificationKind)
        {
            return CanBeAssigned(to, new SimpleESType(from), verificationKind);
        }

        protected void SaveOrCheck(GroboIL il, EvaluationStack stack, GroboIL.Label label)
        {
            ESType[] labelStack;
            if(!il.labelStacks.TryGetValue(label, out labelStack))
            {
                il.labelStacks.Add(label, stack.Reverse().ToArray());
                Propogate(il, il.ilCode.GetLabelLineNumber(label), stack);
            }
            else
            {
                ESType[] merged;
                var comparisonResult = CompareStacks(stack.Reverse().ToArray(), labelStack, out merged);
                switch(comparisonResult)
                {
                case StacksComparisonResult.Equal:
                    return;
                case StacksComparisonResult.Inconsistent:
                    ThrowError(il, string.Format("Inconsistent stack for the label '{0}'{1}Stack #1: {2}{1}Stack #2: {3}", label.Name, Environment.NewLine, stack, new EvaluationStack(labelStack)));
                    break;
                case StacksComparisonResult.Equivalent:
                    il.labelStacks[label] = merged;
                    Propogate(il, il.ilCode.GetLabelLineNumber(label), new EvaluationStack(merged));
                    break;
                }
            }
        }

        protected static Type Canonize(ESType esType)
        {
            return Canonize(esType.ToType());
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

        protected static CLIType ToCLIType(ESType esType)
        {
            if(esType == null)
                return CLIType.Zero;
            var simpleESType = esType as SimpleESType;
            return simpleESType == null ? CLIType.Object : ToCLIType(simpleESType.Type); // ComplexESType is always an object
        }

        private static CLIType ToLowLevelCLIType(ESType esType)
        {
            if(esType == null)
                return CLIType.NativeInt;
            var simpleESType = esType as SimpleESType;
            return simpleESType == null ? CLIType.NativeInt : ToLowLevelCLIType(simpleESType.Type); // ComplexESType is always an object
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
            if(type.IsGenericType) return CLIType.Struct;
            if(type is EnumBuilder) return ToCLIType(type.UnderlyingSystemType);
            return type.IsEnum ? ToCLIType(Enum.GetUnderlyingType(type)) : CLIType.Struct;
        }

        private static CLIType ToLowLevelCLIType(Type type)
        {
            if(type == null)
                return CLIType.NativeInt;
            if(!type.IsValueType)
                return CLIType.NativeInt;
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
            if(type.IsGenericType) return CLIType.Struct;
            if(type is EnumBuilder) return ToLowLevelCLIType(type.UnderlyingSystemType);
            return type.IsEnum ? ToLowLevelCLIType(Enum.GetUnderlyingType(type)) : CLIType.Struct;
        }

        private static bool CanBeAssigned(Type to, ESType esFrom, TypesAssignabilityVerificationKind verificationKind)
        {
            if(verificationKind == TypesAssignabilityVerificationKind.None)
                return true;
            var cliTo = verificationKind == TypesAssignabilityVerificationKind.HighLevel ? ToCLIType(to) : ToLowLevelCLIType(to);
            var cliFrom = verificationKind == TypesAssignabilityVerificationKind.HighLevel ? ToCLIType(esFrom) : ToLowLevelCLIType(esFrom);

            var from = esFrom.ToType();
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
                return ReflectionExtensions.Equal(to, from);
            case CLIType.Pointer:
                if(cliFrom == CLIType.Zero || ReflectionExtensions.Equal(to, from))
                    return true;
                if(cliFrom != CLIType.Pointer)
                    return false;
                to = to.GetElementType();
                from = from.GetElementType();
                return to.IsValueType && from.IsValueType;
            case CLIType.Object:
                if(cliFrom == CLIType.Zero || ReflectionExtensions.Equal(to, from))
                    return true;
                if(cliFrom != CLIType.Object)
                    return false;
                var simpleESFrom = esFrom as SimpleESType;
                if(simpleESFrom != null)
                    return ReflectionExtensions.IsAssignableFrom(to, from);
                var complexESFrom = (ComplexESType)esFrom;
                return ReflectionExtensions.IsAssignableFrom(to, complexESFrom.BaseType) || complexESFrom.Interfaces.Any(interfaCe => ReflectionExtensions.IsAssignableFrom(to, interfaCe));
            case CLIType.Zero:
                return true;
            default:
                throw new InvalidOperationException(string.Format("CLI type '{0}' is not valid at this point", cliTo));
            }
        }

        private static void Propogate(GroboIL il, int lineNumber, EvaluationStack stack)
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
                if(comment is StackILInstructionComment)
                {
                    var instructionStack = ((StackILInstructionComment)comment).Stack;
                    ESType[] merged;
                    var comparisonResult = CompareStacks(stack.Reverse().ToArray(), instructionStack, out merged);
                    switch(comparisonResult)
                    {
                    case StacksComparisonResult.Equal:
                        return;
                    case StacksComparisonResult.Inconsistent:
                        ThrowError(il, string.Format("Inconsistent stack for the line {0}{1}Stack #1: {2}{1}Stack #2: {3}", (lineNumber + 1), Environment.NewLine, stack, new EvaluationStack(instructionStack)));
                        break;
                    case StacksComparisonResult.Equivalent:
                        stack = new EvaluationStack(merged);
                        break;
                    }
                }
                il.ilCode.SetComment(lineNumber, new StackILInstructionComment(stack.Reverse().ToArray()));
                ++lineNumber;
            }
        }

        private static bool EqualESTypes(ESType first, ESType second)
        {
            if((first is SimpleESType) ^ (second is SimpleESType))
                return false;
            if(first.ToType() != second.ToType())
                return false;
            if(first is SimpleESType)
                return true;
            var firstInterfaces = new HashSet<Type>(((ComplexESType)first).Interfaces);
            var secondInterfaces = ((ComplexESType)second).Interfaces;
            foreach(var type in secondInterfaces)
            {
                if(!firstInterfaces.Contains(type))
                    return false;
                firstInterfaces.Remove(type);
            }
            return firstInterfaces.Count == 0;
        }

        private static StacksComparisonResult CompareStacks(ESType[] first, ESType[] second, out ESType[] merged)
        {
            merged = null;
            if(first.Length != second.Length)
                return StacksComparisonResult.Inconsistent;
            ESType[] result = null;
            for(var i = 0; i < first.Length; ++i)
            {
                var firstCLIType = ToCLIType(first[i]);
                var secondCLIType = ToCLIType(second[i]);
                if(firstCLIType != CLIType.Zero && secondCLIType != CLIType.Zero && firstCLIType != secondCLIType)
                    return StacksComparisonResult.Inconsistent;
                if(!EqualESTypes(first[i], second[i]))
                {
                    var common = FindCommonType(firstCLIType, first[i], second[i]);
                    if(common == null)
                        return StacksComparisonResult.Inconsistent;
                    if(result == null)
                    {
                        result = new ESType[first.Length];
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

        private static ESType FindCommonType(CLIType cliType, ESType first, ESType second)
        {
            if(first.ToType() == null) return second;
            if(second.ToType() == null) return first;
            switch(cliType)
            {
            case CLIType.Int32:
                return new SimpleESType(typeof(int));
            case CLIType.Int64:
                return new SimpleESType(typeof(long));
            case CLIType.Float:
                return new SimpleESType(typeof(double));
            case CLIType.NativeInt:
                {
                    if(((SimpleESType)first).Type.IsPointer && ((SimpleESType)second).Type.IsPointer)
                        return new SimpleESType(typeof(void).MakePointerType());
                    return new SimpleESType(typeof(IntPtr));
                }
            case CLIType.Pointer:
                {
                    var firstElementType = ((SimpleESType)first).Type.GetElementType();
                    var secondElementType = ((SimpleESType)second).Type.GetElementType();
                    if(!firstElementType.IsValueType || !secondElementType.IsValueType)
                        return null;
                    return Marshal.SizeOf(firstElementType) <= Marshal.SizeOf(secondElementType) ? first : second;
                }
            case CLIType.Struct:
                return null;
            case CLIType.Object:
                {
                    var baseType = first.ToType().FindBaseClassWith(second.ToType());
                    var firstInterfaces = new HashSet<Type>(new ReflectionExtensions.TypesComparer());
                    if(first is SimpleESType)
                        ((SimpleESType)first).Type.GetInterfacesCollectionStupid(firstInterfaces);
                    else
                    {
                        ((ComplexESType)first).BaseType.GetInterfacesCollectionStupid(firstInterfaces);
                        foreach(var interfaCe in ((ComplexESType)first).Interfaces)
                            firstInterfaces.Add(interfaCe);
                    }
                    var secondInterfaces = new HashSet<Type>(new ReflectionExtensions.TypesComparer());
                    if(second is SimpleESType)
                        ((SimpleESType)second).Type.GetInterfacesCollectionStupid(secondInterfaces);
                    else
                    {
                        ((ComplexESType)second).BaseType.GetInterfacesCollectionStupid(secondInterfaces);
                        foreach(var interfaCe in ((ComplexESType)second).Interfaces)
                            secondInterfaces.Add(interfaCe);
                    }
                    HashSet<Type> intersected;
                    if(firstInterfaces.Count > secondInterfaces.Count)
                    {
                        firstInterfaces.IntersectWith(secondInterfaces);
                        intersected = firstInterfaces;
                    }
                    else
                    {
                        secondInterfaces.IntersectWith(firstInterfaces);
                        intersected = secondInterfaces;
                    }
                    intersected.Add(baseType);

                    //var firstInterfaces = ((first is SimpleESType)
                    //                           ? ((SimpleESType)first).Type.GetTypesArray()
                    //                           : ((ComplexESType)first).BaseType.GetTypesArray().Concat(((ComplexESType)first).Interfaces)).Where(t => t.IsInterface).ToArray();
                    //var secondInterfaces = ((second is SimpleESType)
                    //                            ? ((SimpleESType)second).Type.GetTypesArray()
                    //                            : ((ComplexESType)second).BaseType.GetTypesArray().Concat(((ComplexESType)second).Interfaces)).Where(t => t.IsInterface).ToArray();
                    //var hashSet = new HashSet<Type>(firstInterfaces.Intersect(secondInterfaces).Concat(new[] {baseType}), new ReflectionExtensions.TypesComparer());
                    while(true)
                    {
                        var end = true;
                        foreach(var type in intersected.ToArray())
                        {
                            var children = ReflectionExtensions.GetInterfaces(type);
                            foreach(var child in children)
                            {
                                if(intersected.Contains(child))
                                {
                                    end = false;
                                    intersected.Remove(child);
                                }
                            }
                        }
                        if(end) break;
                    }
                    intersected.Remove(baseType);
                    if(intersected.Count == 0)
                        return new SimpleESType(baseType);
                    return new ComplexESType(baseType, intersected.ToArray());
                }
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