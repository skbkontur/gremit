namespace GrEmit.InstructionParameters
{
    internal class LocalILInstructionParameter : ILInstructionParameter
    {
        public LocalILInstructionParameter(GroboIL.Local local)
        {
            Local = local;
        }

        public override string Format()
        {
            return Local.Name;
        }

        public GroboIL.Local Local { get; private set; }
    }
}