using System;
using System.Reflection.Emit;

namespace GrEmit.StackMutators
{
    internal abstract class BinOpStackMutator : StackMutator
    {
        protected BinOpStackMutator(OpCode opCode)
        {
            this.opCode = opCode;
        }

        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            CheckNotEmpty(il, stack, () => string.Format("Expected two arguments for the operation '{0}'", opCode));
            var right = stack.Pop();
            CheckNotEmpty(il, stack, () => string.Format("Expected two arguments for the operation '{0}'", opCode));
            var left = stack.Pop();
            if(!IsAllowed(ToCLIType(left), ToCLIType(right)))
                ThrowError(il, string.Format("Cannot perform the instruction '{0}' on types '{1}' and '{2}'", opCode, left, right));
            var result = Canonize(GetResultType(Canonize(left), Canonize(right)));
            if(result != typeof(void))
                stack.Push(result);
            PostAction(il, parameter, ref stack);
        }

        protected abstract Type GetResultType(Type left, Type right);
        protected abstract void PostAction(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack);

        protected void Allow(CLIType left, params CLIType[] right)
        {
            foreach(var r in right)
                allowedTypes[(int)left, (int)r] = true;
        }

        private bool IsAllowed(CLIType left, CLIType right)
        {
            return allowedTypes[(int)left, (int)right];
        }

        private readonly bool[,] allowedTypes = new bool[8, 8];
        private readonly OpCode opCode;
    }
}