using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace GrEmit.StackMutators
{
    internal abstract class ConvertStackMutator : StackMutator
    {
        protected ConvertStackMutator(OpCode opCode, Type to)
        {
            this.opCode = opCode;
            this.to = to;
        }

        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            CheckNotEmpty(il, stack);
            var type = stack.Pop();
            if(!Allowed(type))
                ThrowError(il, string.Format("Unable to perform instruction '{0}' on type '{1}'", opCode, Formatter.Format(type)));
            stack.Push(to);
        }

        protected void Allow(params CLIType[] types)
        {
            foreach(var type in types)
                allowed[(int)type] = true;
        }

        private bool Allowed(Type type)
        {
            return allowed[(int)ToCLIType(type)];
        }

        private readonly OpCode opCode;
        private readonly Type to;
        private readonly bool[] allowed = new bool[8];
    }
}