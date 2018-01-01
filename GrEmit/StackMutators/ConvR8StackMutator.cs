using System.Reflection.Emit;

namespace GrEmit.StackMutators
{
    internal class ConvR8StackMutator : ConvertStackMutator
    {
        public ConvR8StackMutator(OpCode opCode)
            : base(opCode, typeof(double))
        {
            Allow(CLIType.Int32, CLIType.Int64, CLIType.NativeInt, CLIType.Float, CLIType.Zero);
        }
    }
}