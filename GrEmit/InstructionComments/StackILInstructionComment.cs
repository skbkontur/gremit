using System.Linq;

using GrEmit.Utils;

namespace GrEmit.InstructionComments
{
    internal class StackILInstructionComment : ILInstructionComment
    {
        public StackILInstructionComment(ESType[] stack)
        {
            Stack = stack;
        }

        public override string Format()
        {
            return Stack == null ? "" : '[' + string.Join(", ", Stack.Select(Formatter.Format).ToArray()) + ']';
        }

        public ESType[] Stack { get; private set; }
    }
}