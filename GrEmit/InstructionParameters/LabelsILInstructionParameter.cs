using System.Linq;

namespace GrEmit.InstructionParameters
{
    internal class LabelsILInstructionParameter : ILInstructionParameter
    {
        public LabelsILInstructionParameter(GroboIL.Label[] labels)
        {
            Labels = labels;
        }

        public override string Format()
        {
            return string.Format("({0})", string.Join(", ", Labels.Select(label => label.Name).ToArray()));
        }

        public GroboIL.Label[] Labels { get; private set; }
    }
}