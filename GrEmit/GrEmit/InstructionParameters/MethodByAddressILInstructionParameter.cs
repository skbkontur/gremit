using System;
using System.Linq;
using System.Reflection;

using GrEmit.Utils;

namespace GrEmit.InstructionParameters
{
    internal class MethodByAddressILInstructionParameter : ILInstructionParameter
    {
        public MethodByAddressILInstructionParameter(CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
        {
            CallingConvention = callingConvention;
            ReturnType = returnType;
            ParameterTypes = parameterTypes;
        }

        public override string Format()
        {
            return Formatter.Format(ReturnType) + " *i" + IntPtr.Size + "(" + string.Join(", ", ParameterTypes.Select(Formatter.Format).ToArray()) + ")";
        }

        public CallingConventions CallingConvention { get; set; }
        public Type ReturnType { get; set; }
        public Type[] ParameterTypes { get; set; }
    }
}