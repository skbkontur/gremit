using System.Reflection.Emit;

namespace GrEmit.StackMutators
{
    internal class ConvI1StackMutator : ConvertStackMutator
    {
        public ConvI1StackMutator(OpCode opCode)
            : base(opCode, typeof(sbyte))
        {
            Allow(CLIType.Int32, CLIType.Int64, CLIType.NativeInt, CLIType.Float, CLIType.Zero);
        }
    }
}