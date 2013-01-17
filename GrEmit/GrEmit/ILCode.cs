using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

using GrEmit.InstructionParameters;

namespace GrEmit
{
    internal class ILCode
    {
        public int MarkLabel(GroboIL.Label label, ILInstructionComment comment)
        {
            labelLineNumbers.Add(label, lineNumber);
            instructions.Add(new ILInstruction(InstructionKind.Label, default(OpCode), new LabelILInstructionParameter(label), comment));
            return lineNumber++;
        }

        public int Append(OpCode opCode, ILInstructionComment comment)
        {
            instructions.Add(new ILInstruction(InstructionKind.Instruction, opCode, null, comment));
            return lineNumber++;
        }

        public int Append(OpCode opCode, ILInstructionParameter parameter, ILInstructionComment comment)
        {
            instructions.Add(new ILInstruction(InstructionKind.Instruction, opCode, parameter, comment));
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

        public ILInstruction GetInstruction(int lineNumber)
        {
            return lineNumber < instructions.Count ? instructions[lineNumber] : null;
        }

        public override string ToString()
        {
            var lines = new List<string>();
            var result = new StringBuilder();
            int maxLen = 0;
            foreach(var instruction in instructions)
            {
                switch(instruction.Kind)
                {
                case InstructionKind.Instruction:
                    lines.Add(margin + instruction.OpCode + (instruction.Parameter == null ? "" : " " + instruction.Parameter.Format()));
                    break;
                case InstructionKind.Label:
                    lines.Add((instruction.Parameter == null ? "" : instruction.Parameter.Format()) + ':');
                    break;
                case InstructionKind.TryStart:
                    lines.Add("TRY");
                    break;
                case InstructionKind.Catch:
                    lines.Add("CATCH <" + (instruction.Parameter == null ? "" : instruction.Parameter.Format()) + '>');
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
                if(maxLen < lines[lines.Count - 1].Length)
                    maxLen = lines[lines.Count - 1].Length;
            }
            if(maxLen > maxCommentStart)
                maxLen = maxCommentStart;
            InitMargins(maxCommentStart + margin.Length);
            maxLen += margin.Length;
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

        internal class ILInstruction
        {
            public ILInstruction(InstructionKind kind, OpCode opCode, ILInstructionParameter parameter, ILInstructionComment comment)
            {
                Kind = kind;
                OpCode = opCode;
                Parameter = parameter;
                Comment = comment;
            }

            public InstructionKind Kind { get; set; }
            public OpCode OpCode { get; private set; }
            public ILInstructionParameter Parameter { get; private set; }
            public ILInstructionComment Comment { get; set; }
        }

        internal enum InstructionKind
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

        private readonly List<ILInstruction> instructions = new List<ILInstruction>();

        private readonly Dictionary<Label, int> labelLineNumbers = new Dictionary<Label, int>();

        private int lineNumber;
        private const int maxCommentStart = 50;
        private const string margin = "        ";
    }
}