using System;

namespace GrEmit.InstructionParameters
{
    public class TypeILInstructionParameter : ILInstructionParameter
    {
        public TypeILInstructionParameter(Type type)
        {
            if(type == typeof(IntPtr))
                Type = IntPtr.Size == 4 ? typeof(int) : typeof(long);
            else if(type == typeof(UIntPtr))
                Type = IntPtr.Size == 4 ? typeof(uint) : typeof(ulong);
            else
                Type = type;
        }

        public override string Format()
        {
            return Formatter.Format(Type);
        }

        public Type Type { get; set; }
    }
}