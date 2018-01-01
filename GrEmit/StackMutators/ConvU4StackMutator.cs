using System.Reflection.Emit;

namespace GrEmit.StackMutators
{
    internal class ConvU4StackMutator : ConvertStackMutator
    {
        public ConvU4StackMutator(OpCode opCode)
            : base(opCode, typeof(uint))
        {
            Allow(CLIType.Int32, CLIType.Int64, CLIType.NativeInt, CLIType.Float, CLIType.Zero);
        }
    }
}