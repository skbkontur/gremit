using System.Linq;

using GrEmit.InstructionParameters;

namespace GrEmit.StackMutators
{
    internal class BrfalseStackMutator : StackMutator
    {
        public override void Mutate(GroboIL il, ILInstructionParameter parameter, ref EvaluationStack stack)
        {
            var label = ((LabelILInstructionParameter)parameter).Label;
            CheckNotEmpty(il, stack, () => "The 'brfalse' instruction required one argument but none is supplied");
            var value = stack.Pop();
            CheckNotStruct(il, value);

            var newStack = stack.Reverse().ToArray();
            for(var i = 0; i < newStack.Length; ++i)
            {
                if(ReferenceEquals(newStack[i], value))
                    newStack[i] = ESType.Zero;
            }

            SaveOrCheck(il, new EvaluationStack(newStack), label);
        }
    }
}