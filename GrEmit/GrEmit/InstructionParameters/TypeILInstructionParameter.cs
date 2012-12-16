using System;

namespace GrEmit.InstructionParameters
{
    internal class TypeILInstructionParameter : ILInstructionParameter
    {
        public TypeILInstructionParameter(Type type)
        {
            Type = type;
        }

        public override string Format()
        {
            return Formatter.Format(Type);
        }

        public Type Type { get; set; }
    }
}