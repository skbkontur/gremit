using System.Reflection.Emit;

namespace GrEmit.StackMutators
{
    internal class ConvI2StackMutator : ConvertStackMutator
    {
        public ConvI2StackMutator(OpCode opCode)
            : base(opCode, typeof(short))
        {
            Allow(CLIType.Int32, CLIType.Int64, CLIType.NativeInt, CLIType.Float, CLIType.Zero);
        }
    }
}