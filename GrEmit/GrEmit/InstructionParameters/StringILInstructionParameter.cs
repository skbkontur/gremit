namespace GrEmit.InstructionParameters
{
    internal class StringILInstructionParameter : ILInstructionParameter
    {
        public StringILInstructionParameter(string value)
        {
            this.value = value;
        }

        public override string Format()
        {
            return '\'' + value + '\'';
        }

        private readonly string value;
    }
}