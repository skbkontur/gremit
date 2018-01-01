using System;

using GrEmit.Utils;

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
            return Type == null ? "null" : Formatter.Format(Type);
        }

        public Type Type { get; private set; }
    }
}