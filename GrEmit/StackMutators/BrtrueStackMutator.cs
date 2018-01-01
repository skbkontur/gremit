using System.Linq;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class BrtrueStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            var label = ((LabelILInstructionParameter)parameter).Label;
            CheckNotEmpty(il, stack, () => "The 'brtrue' instruction requires one argument but none is supplied");
            var value = stack.Pop();
            CheckNotStruct(il, value);

            var newStack = stack.Reverse().ToArray();
            for(var i = 0; i < newStack.Length; ++i)
            {
                if(ReferenceEquals(newStack[i], value))
                    newStack[i] = ESType.Zero;
            }

            SaveOrCheck(il, stack, label);

            stack = new EvaluationStack(newStack);
        }
    }
}