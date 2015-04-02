using System;
using System.Collections.Generic;
using System.Reflection.Emit;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal abstract class BranchStackMutator : BinOpStackMutator
    {
        protected BranchStackMutator(OpCode opCode)
            : base(opCode)
        {
        }

        protected override Type GetResultType(Type left, Type right)
        {
            return typeof(void);
        }

        protected override void PostAction(GroboIL il, ILInstructionParameter parameter, ref Stack<Type> stack)
        {
            SaveOrCheck(il, stack, ((LabelILInstructionParameter)parameter).Label);
        }
    }
}