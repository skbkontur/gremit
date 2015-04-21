namespace GrEmit.InstructionParameters
{
    internal class StringILInstructionParameter : ILInstructionParameter
    {
        public string Value { get; private set; }

        public StringILInstructionParameter(string value)
        {
            Value = value;
        }

        public override string Format()
        {
            return '\'' + Value + '\'';
        }
    }
}