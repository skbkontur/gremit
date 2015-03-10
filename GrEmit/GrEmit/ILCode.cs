using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

using GrEmit.InstructionParameters;

namespace GrEmit
{
    public class ILCode
    {
        public int MarkLabel(GroboIL.Label label, ILInstructionComment comment)
        {
            labelLineNumbers.Add(label, lineNumber);
            instructions.Add(new ILInstruction(InstructionKind.Label, default(OpCode), new LabelILInstructionParameter(label), comment));
            return lineNumber++;
        }

        public int Append(OpCode opCode, ILInstructionComment comment)
        {
            var lastInstructionPrefix = lineNumber > 0 ? instructions[lineNumber - 1] as ILInstructionPrefix : null;
            if(lastInstructionPrefix == null)
            {
                instructions.Add(new ILInstruction(InstructionKind.Instruction, opCode, null, comment));
                return lineNumber++;
            }
            instructions[lineNumber - 1] = new ILInstruction(InstructionKind.Instruction, opCode, null, comment) {Prefixes = lastInstructionPrefix.Prefixes};
            return lineNumber - 1;
        }

        public int Append(OpCode opCode, ILInstructionParameter parameter, ILInstructionComment comment)
        {
            var lastInstructionPrefix = lineNumber > 0 ? instructions[lineNumber - 1] as ILInstructionPrefix : null;
            if (lastInstructionPrefix == null)
            {
                instructions.Add(new ILInstruction(InstructionKind.Instruction, opCode, parameter, comment));
                return lineNumber++;
            }
            instructions[lineNumber - 1] = new ILInstruction(InstructionKind.Instruction, opCode, null, comment) {Prefixes = lastInstructionPrefix.Prefixes};
            return lineNumber - 1;
        }

        public int AppendPrefix(OpCode prefix)
        {
            var lastInstructionPrefix = instructions[lineNumber - 1] as ILInstructionPrefix;
            if(lastInstructionPrefix != null)
            {
                lastInstructionPrefix.Prefixes.Add(prefix);
                return lineNumber - 1;
            }
            instructions.Add(new ILInstructionPrefix {Prefixes = new List<OpCode> {prefix}});
            return lineNumber++;
        }

        public int BeginExceptionBlock(ILInstructionComment comment)
        {
            instructions.Add(new ILInstruction(InstructionKind.TryStart, default(OpCode), null, comment));
            return lineNumber++;
        }

        public int BeginCatchBlock(TypeILInstructionParameter parameter, ILInstructionComment comment)
        {
            instructions.Add(new ILInstruction(InstructionKind.Catch, default(OpCode), parameter, comment));
            return lineNumber++;
        }

        public int BeginExceptFilterBlock(ILInstructionComment comment)
        {
            instructions.Add(new ILInstruction(InstructionKind.FilteredException, default(OpCode), null, comment));
            return lineNumber++;
        }

        public int BeginFaultBlock(ILInstructionComment comment)
        {
            instructions.Add(new ILInstruction(InstructionKind.Fault, default(OpCode), null, comment));
            return lineNumber++;
        }

        public int BeginFinallyBlock(ILInstructionComment comment)
        {
            instructions.Add(new ILInstruction(InstructionKind.Finally, default(OpCode), null, comment));
            return lineNumber++;
        }

        public int EndExceptionBlock(ILInstructionComment comment)
        {
            instructions.Add(new ILInstruction(InstructionKind.TryEnd, default(OpCode), null, comment));
            return lineNumber++;
        }

        public int GetLabelLineNumber(GroboIL.Label label)
        {
            int result;
            return labelLineNumbers.TryGetValue(label, out result) ? result : -1;
        }

        public ILInstructionComment GetComment(int lineNumber)
        {
            return lineNumber < instructions.Count ? instructions[lineNumber].Comment : null;
        }

        public void SetComment(int lineNumber, ILInstructionComment comment)
        {
            if(lineNumber < instructions.Count)
                instructions[lineNumber].Comment = comment;
        }

        public ILInstructionBase GetInstruction(int lineNumber)
        {
            return lineNumber < instructions.Count ? instructions[lineNumber] : null;
        }

        public override string ToString()
        {
            var lines = new List<string>();
            int maxLen = 0;
            foreach(var instruction in instructions)
            {
                var ilInstruction = instruction as ILInstruction;
                if(ilInstruction != null)
                {
                    switch(ilInstruction.Kind)
                    {
                    case InstructionKind.Instruction:
                        lines.Add(margin + string.Join("", (ilInstruction.Prefixes ?? new List<OpCode>()).Concat(new[] {ilInstruction.OpCode}).Select(opcode => opcode.ToString()).ToArray()) + (ilInstruction.Parameter == null ? "" : " " + ilInstruction.Parameter.Format()));
                        break;
                    case InstructionKind.Label:
                        lines.Add((ilInstruction.Parameter == null ? "" : ilInstruction.Parameter.Format()) + ':');
                        break;
                    case InstructionKind.TryStart:
                        lines.Add("TRY");
                        break;
                    case InstructionKind.Catch:
                        lines.Add("CATCH <" + (ilInstruction.Parameter == null ? "" : ilInstruction.Parameter.Format()) + '>');
                        break;
                    case InstructionKind.FilteredException:
                        lines.Add("CATCH");
                        break;
                    case InstructionKind.Fault:
                        lines.Add("FAULT");
                        break;
                    case InstructionKind.Finally:
                        lines.Add("FINALLY");
                        break;
                    case InstructionKind.TryEnd:
                        lines.Add("END TRY");
                        break;
                    }
                }
                else
                {
                    var prefix = (ILInstructionPrefix)instruction;
                    lines.Add(margin + string.Join("", prefix.Prefixes.Select(opcode => opcode.ToString()).ToArray()) + ".");
                }
                if(maxLen < lines[lines.Count - 1].Length)
                    maxLen = lines[lines.Count - 1].Length;
            }
            if(maxLen > maxCommentStart)
                maxLen = maxCommentStart;
            InitMargins(maxCommentStart + margin.Length);
            maxLen += margin.Length;
            var result = new StringBuilder();
            for(int i = 0; i < instructions.Count; ++i)
            {
                string line = lines[i];
                result.Append(line);
                ILInstructionComment comment = instructions[i].Comment;
                if(comment != null)
                {
                    if(line.Length <= maxLen)
                    {
                        result.Append(WhiteSpace(maxLen - line.Length));
                        result.Append("// ");
                        result.Append(comment.Format());
                    }
                    else
                    {
                        result.AppendLine();
                        result.Append(WhiteSpace(maxLen));
                        result.Append("// ");
                        result.Append(comment.Format());
                    }
                }
                result.AppendLine();
            }
            return result.ToString();
        }

        public int Count { get { return lineNumber; } }

        public class ILInstructionBase
        {
            public ILInstructionComment Comment { get; set; }
        }

        public class ILInstruction : ILInstructionBase
        {
            public ILInstruction(InstructionKind kind, OpCode opCode, ILInstructionParameter parameter, ILInstructionComment comment)
            {
                Kind = kind;
                OpCode = opCode;
                Parameter = parameter;
                Comment = comment;
            }

            public InstructionKind Kind { get; set; }
            public OpCode OpCode { get; set; }
            public ILInstructionParameter Parameter { get; private set; }
            public List<OpCode> Prefixes { get; set; }
        }

        public class ILInstructionPrefix : ILInstructionBase
        {
            public List<OpCode> Prefixes { get; set; }
        }

        public enum InstructionKind
        {
            Instruction,
            Label,
            TryStart,
            TryEnd,
            Catch,
            FilteredException,
            Finally,
            Fault
        }

        private static void InitMargins(int length)
        {
            if(margins == null)
            {
                margins = new string[length + 1];
                margins[0] = "";
                for(int i = 1; i <= length; ++i)
                    margins[i] = new string(' ', i);
            }
        }

        private static string WhiteSpace(int length)
        {
            return margins[length];
        }

        private static string[] margins;

        private readonly List<ILInstructionBase> instructions = new List<ILInstructionBase>();

        private readonly Dictionary<Label, int> labelLineNumbers = new Dictionary<Label, int>();

        private int lineNumber;
        private const int maxCommentStart = 50;
        private const string margin = "        ";
    }
}