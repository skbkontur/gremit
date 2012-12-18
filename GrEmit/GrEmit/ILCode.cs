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
            instructions.Add(new ILInstruction(default(OpCode), new LabelILInstructionParameter(label), comment));
            return lineNumber++;
        }

        public int Append(OpCode opCode, ILInstructionComment comment)
        {
            instructions.Add(new ILInstruction(opCode, null, comment));
            return lineNumber++;
        }

        public int Append(OpCode opCode, ILInstructionParameter parameter, ILInstructionComment comment)
        {
            instructions.Add(new ILInstruction(opCode, parameter, comment));
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

        private static string[] margins;

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

        public override string ToString()
        {
            var lines = new List<string>();
            var result = new StringBuilder();
            int maxLen = 0;
            foreach(var instruction in instructions)
            {
                var opCode = instruction.OpCode;
                if(opCode == default(OpCode))
                    lines.Add((instruction.Parameter == null ? "" : instruction.Parameter.Format()) + ':');
                else
                    lines.Add(margin + opCode + (instruction.Parameter == null ? "" : " " + instruction.Parameter.Format()));
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
                    if(line.Length < maxLen)
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
            public ILInstruction(OpCode opCode, ILInstructionParameter parameter, ILInstructionComment comment)
            {
                OpCode = opCode;
                Parameter = parameter;
                Comment = comment;
            }

            public OpCode OpCode { get; private set; }
            public ILInstructionParameter Parameter { get; private set; }
            public ILInstructionComment Comment { get; set; }
        }

        private readonly List<ILInstruction> instructions = new List<ILInstruction>();

        private readonly Dictionary<Label, int> labelLineNumbers = new Dictionary<Label, int>();

        private int lineNumber;
        private const int maxCommentStart = 50;
        private const string margin = "        ";
    }
}