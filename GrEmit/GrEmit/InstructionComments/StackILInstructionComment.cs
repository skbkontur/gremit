using System;
using System.Linq;

namespace GrEmit.InstructionComments
{
    internal class StackILInstructionComment : ILInstructionComment
    {
        public StackILInstructionComment(Type[] stack)
        {
            Stack = stack;
        }

        public override string Format()
        {
            return Stack == null ? "" : '[' + string.Join(", ", Stack.Select(Formatter.Format)) + ']';
        }

        public Type[] Stack { get; private set; }
    }
}