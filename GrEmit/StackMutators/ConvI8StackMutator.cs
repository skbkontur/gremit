using System.Reflection.Emit;

namespace GrEmit.StackMutators
{
    internal class ConvI8StackMutator : ConvertStackMutator
    {
        public ConvI8StackMutator(OpCode opCode)
            : base(opCode, typeof(long))
        {
            Allow(CLIType.Int32, CLIType.Int64, CLIType.NativeInt, CLIType.Float, CLIType.Pointer, CLIType.Object, CLIType.Zero);
        }
    }
}