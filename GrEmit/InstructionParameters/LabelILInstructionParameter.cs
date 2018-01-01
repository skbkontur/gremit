namespace GrEmit.InstructionParameters
{
    internal class LabelILInstructionParameter : ILInstructionParameter
    {
        public LabelILInstructionParameter(GroboIL.Label label)
        {
            Label = label;
        }

        public override string Format()
        {
            return Label.Name;
        }

        public GroboIL.Label Label { get; private set; }
    }
}