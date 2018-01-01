using System.Reflection.Emit;

namespace GrEmit.StackMutators
{
    internal abstract class ArithmeticBinOpStackMutator : BinOpStackMutator
    {
        protected ArithmeticBinOpStackMutator(OpCode opCode)
            : base(opCode)
        {
        }

        protected override void PostAction(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
        }
    }
}