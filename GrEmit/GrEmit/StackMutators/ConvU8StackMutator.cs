using System.Reflection.Emit;

namespace GrEmit.StackMutators
{
    internal class ConvU8StackMutator : ConvertStackMutator
    {
        public ConvU8StackMutator(OpCode opCode)
            : base(opCode, typeof(ulong))
        {
            Allow(CLIType.Int32, CLIType.Int64, CLIType.NativeInt, CLIType.Float, CLIType.Pointer, CLIType.Object, CLIType.Zero);
        }
    }
}