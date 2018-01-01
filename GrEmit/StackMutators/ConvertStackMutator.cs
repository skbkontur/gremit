using System;
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

        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            CheckNotEmpty(il, stack, () => string.Format("In order to perform the instruction '{0}' an instance must be loaded onto the evaluation stack", opCode));
            var type = stack.Pop();
            if(!Allowed(type))
                ThrowError(il, string.Format("Unable to perform the instruction '{0}' on type '{1}'", opCode, type));
            stack.Push(to);
        }

        protected void Allow(params CLIType[] types)
        {
            foreach(var type in types)
                allowed[(int)type] = true;
        }

        private bool Allowed(ESType type)
        {
            return allowed[(int)ToCLIType(type)];
        }

        private readonly OpCode opCode;
        private readonly Type to;
        private readonly bool[] allowed = new bool[8];
    }
}