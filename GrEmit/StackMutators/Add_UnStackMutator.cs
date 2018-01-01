using System;
using System.Reflection.Emit;

namespace GrEmit.StackMutators
{
    internal class Add_UnStackMutator : ArithmeticBinOpStackMutator
    {
        public Add_UnStackMutator(OpCode opCode)
            : base(opCode)
        {
            Allow(CLIType.Int32, CLIType.Int32, CLIType.NativeInt, CLIType.Pointer, CLIType.Zero);
            Allow(CLIType.Int64, CLIType.Int64, CLIType.Zero);
            Allow(CLIType.NativeInt, CLIType.Int32, CLIType.NativeInt, CLIType.Pointer, CLIType.Zero);
            Allow(CLIType.Float, CLIType.Float, CLIType.Zero);
            Allow(CLIType.Pointer, CLIType.Int32, CLIType.NativeInt, CLIType.Zero);
            Allow(CLIType.Zero, CLIType.Int32, CLIType.Int64, CLIType.NativeInt, CLIType.Float, CLIType.Pointer, CLIType.Zero);
        }

        protected override Type GetResultType(Type left, Type right)
        {
            if(left == null) return right; // zero + type = type
            if(right == null) return left; // type + zero = type
            if(left == typeof(int) && right == typeof(int)) // int + int = int
                return typeof(int);
            if(left == typeof(long) && right == typeof(long)) // long + long = long
                return typeof(long);
            if(left == typeof(float) && right == typeof(float)) // float + float = float
                return typeof(float);
            if(left == typeof(double) || right == typeof(double)) // double + float = float + double = double + double = double
                return typeof(double);
            var leftCLIType = ToCLIType(left);
            var rightCLIType = ToCLIType(right);
            if(leftCLIType == CLIType.Pointer) // ref type + int = ref type
                return left;
            if(rightCLIType == CLIType.Pointer) // int + ref type = ref type
                return right;
            return typeof(IntPtr); // int + native int = native int + int = native int + native int = native int
        }
    }
}