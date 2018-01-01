using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class CastclassStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            // todo test ALL!
            var to = ((TypeILInstructionParameter)parameter).Type;
            CheckNotEmpty(il, stack, () => "In order to perform the 'castclass' instruction an instance must be loaded onto the evaluation stack");
            var from = stack.Pop().ToType();
            if(!(ToCLIType(from) == CLIType.Object && CanBeAssigned(to, from, il.VerificationKind)))
                CheckCanBeAssigned(il, from, to);
            stack.Push(to);
        }
    }
}