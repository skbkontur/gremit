using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using GrEmit.Utils;

namespace GrEmit.InstructionParameters
{
    internal class MethodByAddressILInstructionParameter : ILInstructionParameter
    {
        public MethodByAddressILInstructionParameter(CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
        {
            ManagedCallingConvention = callingConvention;
            ReturnType = returnType;
            ParameterTypes = parameterTypes;
        }

        public MethodByAddressILInstructionParameter(CallingConvention callingConvention, Type returnType, Type[] parameterTypes)
        {
            UnmanagedCallingConvention = callingConvention;
            ReturnType = returnType;
            ParameterTypes = parameterTypes;
        }

        public override string Format()
        {
            return Formatter.Format(ReturnType) + " *i" + IntPtr.Size + "(" + string.Join(", ", ParameterTypes.Select(Formatter.Format).ToArray()) + ")";
        }

        public CallingConventions? ManagedCallingConvention { get; private set; }
        public CallingConvention? UnmanagedCallingConvention { get; private set; }
        public Type ReturnType { get; private set; }
        public Type[] ParameterTypes { get; private set; }
    }
}