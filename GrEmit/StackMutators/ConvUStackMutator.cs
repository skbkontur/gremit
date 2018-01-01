using System;
using System.Reflection.Emit;

namespace GrEmit.StackMutators
{
    internal class ConvUStackMutator : ConvertStackMutator
    {
        public ConvUStackMutator()
            : base(OpCodes.Conv_U, typeof(UIntPtr))
        {
            Allow(CLIType.Int32, CLIType.Int64, CLIType.NativeInt, CLIType.Float, CLIType.Pointer, CLIType.Object, CLIType.Zero);
        }
    }
}