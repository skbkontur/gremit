using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class CastclassStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            // todo проверить ВСЕ!
            var to = ((TypeILInstructionParameter)parameter).Type;
            CheckNotEmpty(il, stack);
            var from = stack.Pop().ToType();
            if(!(ToCLIType(from) == CLIType.Object && CanBeAssigned(to, from)))
                CheckCanBeAssigned(il, from, to);
            stack.Push(to);
        }
    }
}