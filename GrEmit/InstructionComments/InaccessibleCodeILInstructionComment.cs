namespace GrEmit.InstructionComments
{
    internal class InaccessibleCodeILInstructionComment : ILInstructionComment
    {
        public override string Format()
        {
            return "WARNING: Inaccessible instruction";
        }
    }
}