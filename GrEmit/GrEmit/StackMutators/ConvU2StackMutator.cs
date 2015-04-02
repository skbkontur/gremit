using System.Reflection.Emit;

namespace GrEmit.StackMutators
{
    internal class ConvU2StackMutator : ConvertStackMutator
    {
        public ConvU2StackMutator(OpCode opCode)
            : base(opCode, typeof(ushort))
        {
            Allow(CLIType.Int32, CLIType.Int64, CLIType.NativeInt, CLIType.Float, CLIType.Zero);
        }
    }
}