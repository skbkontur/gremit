namespace GrEmit.InstructionParameters
{
    internal class StringILInstructionParameter : ILInstructionParameter
    {
        public StringILInstructionParameter(string value)
        {
            Value = value;
        }

        public override string Format()
        {
            return '\'' + Value + '\'';
        }

        public string Value { get; private set; }
    }
}