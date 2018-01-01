using System;
using System.Reflection.Emit;

namespace GrEmit.StackMutators
{
    internal class ConvIStackMutator : ConvertStackMutator
    {
        public ConvIStackMutator()
            : base(OpCodes.Conv_I, typeof(IntPtr))
        {
            Allow(CLIType.Int32, CLIType.Int64, CLIType.NativeInt, CLIType.Float, CLIType.Pointer, CLIType.Object, CLIType.Zero);
        }
    }
}