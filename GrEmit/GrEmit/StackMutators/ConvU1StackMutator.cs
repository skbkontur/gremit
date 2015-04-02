using System.Reflection.Emit;

namespace GrEmit.StackMutators
{
    internal class ConvU1StackMutator : ConvertStackMutator
    {
        public ConvU1StackMutator(OpCode opCode)
            : base(opCode, typeof(byte))
        {
            Allow(CLIType.Int32, CLIType.Int64, CLIType.NativeInt, CLIType.Float, CLIType.Zero);
        }
    }
}