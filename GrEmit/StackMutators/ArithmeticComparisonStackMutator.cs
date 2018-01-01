using System;
using System.Reflection.Emit;

namespace GrEmit.StackMutators
{
    internal class ArithmeticComparisonStackMutator : ArithmeticBinOpStackMutator
    {
        public ArithmeticComparisonStackMutator(OpCode opCode)
            : base(opCode)
        {
            Allow(CLIType.Int32, CLIType.Int32, CLIType.NativeInt, CLIType.Zero);
            Allow(CLIType.Int64, CLIType.Int64, CLIType.Zero);
            Allow(CLIType.NativeInt, CLIType.Int32, CLIType.NativeInt, CLIType.Zero);
            Allow(CLIType.Float, CLIType.Float, CLIType.Zero);
            Allow(CLIType.Pointer, CLIType.Pointer, CLIType.Zero);
            Allow(CLIType.Zero, CLIType.Int32, CLIType.Int64, CLIType.NativeInt, CLIType.Float, CLIType.Pointer, CLIType.Zero);
        }

        protected override Type GetResultType(Type left, Type right)
        {
            return typeof(int);
        }
    }
}