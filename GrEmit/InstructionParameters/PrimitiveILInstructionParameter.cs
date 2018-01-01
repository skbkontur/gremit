namespace GrEmit.InstructionParameters
{
    internal class PrimitiveILInstructionParameter : ILInstructionParameter
    {
        public PrimitiveILInstructionParameter(object value)
        {
            Value = value;
        }

        public override string Format()
        {
            return Value.ToString();
        }

        public object Value { get; private set; }
    }
}