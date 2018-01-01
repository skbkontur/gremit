using System;
using System.Reflection.Emit;

namespace GrEmit.StackMutators
{
    internal class NumericOpStackMutator : ArithmeticBinOpStackMutator
    {
        public NumericOpStackMutator(OpCode opCode)
            : base(opCode)
        {
            Allow(CLIType.Int32, CLIType.Int32, CLIType.NativeInt, CLIType.Zero);
            Allow(CLIType.Int64, CLIType.Int64, CLIType.Zero);
            Allow(CLIType.NativeInt, CLIType.Int32, CLIType.NativeInt, CLIType.Zero);
            Allow(CLIType.Float, CLIType.Float, CLIType.Zero);
            Allow(CLIType.Zero, CLIType.Int32, CLIType.Int64, CLIType.NativeInt, CLIType.Float, CLIType.Zero);
        }

        protected override Type GetResultType(Type left, Type right)
        {
            if(left == null) return right; // zero op type = type
            if(right == null) return left; // type op zero = type
            if(left == typeof(int) && right == typeof(int)) // int op int = int
                return typeof(int);
            if(left == typeof(long) && right == typeof(long)) // long op long = long
                return typeof(long);
            if(left == typeof(float) && right == typeof(float)) // float op float = float
                return typeof(float);
            if(left == typeof(double) || right == typeof(double)) // double op float = float op double = double op double = double
                return typeof(double);
            return typeof(IntPtr); // int op native int = native int op int = native int op native int = native int
        }
    }
}