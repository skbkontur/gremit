using System.Reflection.Emit;

namespace GrEmit.StackMutators
{
    internal class ConvR4StackMutator : ConvertStackMutator
    {
        public ConvR4StackMutator(OpCode opCode)
            : base(opCode, typeof(float))
        {
            Allow(CLIType.Int32, CLIType.Int64, CLIType.NativeInt, CLIType.Float, CLIType.Zero);
        }
    }
}