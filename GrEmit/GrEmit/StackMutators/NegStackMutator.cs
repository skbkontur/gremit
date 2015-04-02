using System;
using System.Collections.Generic;

namespace GrEmit.StackMutators
{
    internal class NegStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            CheckNotEmpty(il, stack);
            var type = stack.Pop();
            var cliType = ToCLIType(type);
            if(cliType != CLIType.Int32 && cliType != CLIType.Int64 && cliType != CLIType.NativeInt && cliType != CLIType.Float && cliType != CLIType.Zero)
                ThrowError(il, string.Format("Unable to perform 'neg' opertation on type '{0}'", Formatter.Format(type)));
            stack.Push(Canonize(type));
        }
    }
}