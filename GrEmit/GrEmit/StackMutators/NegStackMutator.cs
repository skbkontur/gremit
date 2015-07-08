namespace GrEmit.StackMutators
{
    internal class NegStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            CheckNotEmpty(il, stack);
            var esType = stack.Pop();
            var cliType = ToCLIType(esType);
            if(cliType != CLIType.Int32 && cliType != CLIType.Int64 && cliType != CLIType.NativeInt && cliType != CLIType.Float && cliType != CLIType.Zero)
                ThrowError(il, string.Format("Unable to perform 'neg' operation on type '{0}'", esType));
            stack.Push(Canonize(esType));
        }
    }
}