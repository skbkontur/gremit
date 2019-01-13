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
            CheckNotEmpty(il, stack, () => $"In order to perform the instruction '{opCode}' an instance must be loaded onto the evaluation stack");
            var type = stack.Pop();
            if (!Allowed(type))
                ThrowError(il, $"Unable to perform the instruction '{opCode}' on type '{type}'");
            stack.Push(to);
        }

        protected void Allow(params CLIType[] types)
        {
            foreach (var type in types)
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