using System;
using System.Reflection;

namespace GrEmit.InstructionParameters
{
    internal class CallILInstructionParameter : MethodILInstructionParameter
    {
        public CallILInstructionParameter(MethodInfo method, Type constrained)
            : base(method)
        {
            Constrained = constrained;
        }

        public Type Constrained { get; private set; }
    }
}