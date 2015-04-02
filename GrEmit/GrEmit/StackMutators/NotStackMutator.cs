using System;
using System.Collections.Generic;

namespace GrEmit.StackMutators
{
    internal class NotStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            CheckNotEmpty(il, stack);
            var type = stack.Pop();
            var cliType = ToCLIType(type);
            if(cliType != CLIType.Int32 && cliType != CLIType.Int64 && cliType != CLIType.NativeInt && cliType != CLIType.Zero)
                ThrowError(il, string.Format("Unable to perform 'not' opertation on type '{0}'", Formatter.Format(type)));
            // !zero = -1 -> native int
            stack.Push(cliType == CLIType.Zero ? typeof(IntPtr) : Canonize(type));
        }
    }
}