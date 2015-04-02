using System;
using System.Reflection.Emit;

namespace GrEmit.StackMutators
{
    internal class ShiftOpStackMutator : ArithmeticBinOpStackMutator
    {
        public ShiftOpStackMutator(OpCode opCode)
            : base(opCode)
        {
            Allow(CLIType.Int32, CLIType.Int32, CLIType.NativeInt, CLIType.Zero);
            Allow(CLIType.Int64, CLIType.Int64, CLIType.NativeInt, CLIType.Zero);
            Allow(CLIType.NativeInt, CLIType.Int32, CLIType.NativeInt, CLIType.Zero);
            Allow(CLIType.Zero, CLIType.Int32, CLIType.NativeInt, CLIType.Zero);
        }

        protected override Type GetResultType(Type left, Type right)
        {
            return left;
        }
    }
}