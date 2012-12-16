using System;
using System.Linq;

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
            return Formatter.Format(ReturnType) + "&(" + string.Join(", ", ParameterTypes.Select(Formatter.Format)) + ")";
        }

        public Type ReturnType { get; set; }
        public Type[] ParameterTypes { get; set; }
    }
}