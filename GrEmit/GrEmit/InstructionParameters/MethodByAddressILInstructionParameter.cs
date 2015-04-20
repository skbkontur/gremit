using System;
using System.Linq;

using GrEmit.Utils;

namespace GrEmit.InstructionParameters
{
    internal class MethodByAddressILInstructionParameter : ILInstructionParameter
    {
        public MethodByAddressILInstructionParameter(Type returnType, Type[] parameterTypes)
        {
            ReturnType = returnType;
            ParameterTypes = parameterTypes;
        }

        public override string Format()
        {
            return Formatter.Format(ReturnType) + " *i" + IntPtr.Size + "(" + string.Join(", ", ParameterTypes.Select(Formatter.Format).ToArray()) + ")";
        }

        public Type ReturnType { get; set; }
        public Type[] ParameterTypes { get; set; }
    }
}