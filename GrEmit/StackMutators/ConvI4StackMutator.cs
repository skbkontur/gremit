using System.Reflection.Emit;

namespace GrEmit.StackMutators
{
    internal class ConvI4StackMutator : ConvertStackMutator
    {
        public ConvI4StackMutator(OpCode opCode)
            : base(opCode, typeof(int))
        {
            Allow(CLIType.Int32, CLIType.Int64, CLIType.NativeInt, CLIType.Float, CLIType.Zero);
        }
    }
}