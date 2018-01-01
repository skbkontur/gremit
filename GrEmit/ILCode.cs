using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

using GrEmit.InstructionParameters;

namespace GrEmit
{
    internal class ILCode
    {
        public int MarkLabel(GroboIL.Label label, ILInstructionComment comment)
        {
            if(labelLineNumbers.ContainsKey(label))
                throw new InvalidOperationException(string.Format("The label '{0}' has already been marked", label.Name));
            labelLineNumbers.Add(label, Count);
            instructions.Add(new ILInstruction(InstructionKind.Label, default(OpCode), new LabelILInstructionParameter(label), comment));
            return Count++;
        }

        public void CheckLabels()
        {
            foreach(var instruction in instructions.Cast<ILInstruction>())
            {
                if(instruction.Parameter is LabelILInstructionParameter)
                {
                    var label = (LabelILInstructionParameter)instruction.Parameter;
                    if(!labelLineNumbers.ContainsKey(label.Label))
                        throw new InvalidOperationException(string.Format("The label '{0}' has not been marked", label.Label.Name));
                }
                if(instruction.Parameter is LabelsILInstructionParameter)
                {
                    foreach(var label in ((LabelsILInstructionParameter)instruction.Parameter).Labels)
                    {
                        if(!labelLineNumbers.ContainsKey(label))
                            throw new InvalidOperationException(string.Format("The label '{0}' has not been marked", label.Name));
                    }
                }
            }
        }

        public int Append(OpCode opCode, ILInstructionComment comment)
        {
            var lastInstructionPrefix = Count > 0 ? instructions[Count - 1] as ILInstructionPrefix : null;
            if(lastInstructionPrefix == null)
            {
                instructions.Add(new ILInstruction(InstructionKind.Instruction, opCode, null, comment));
                return Count++;
            }
            instructions[Count - 1] = new ILInstruction(InstructionKind.Instruction, opCode, null, comment) {Prefixes = lastInstructionPrefix.Prefixes};
            return Count - 1;
        }

        public int Append(OpCode opCode, ILInstructionParameter parameter, ILInstructionComment comment)
        {
            var lastInstructionPrefix = Count > 0 ? instructions[Count - 1] as ILInstructionPrefix : null;
            if(lastInstructionPrefix == null)
            {
                instructions.Add(new ILInstruction(InstructionKind.Instruction, opCode, parameter, comment));
                return Count++;
            }
            instructions[Count - 1] = new ILInstruction(InstructionKind.Instruction, opCode, parameter, comment) {Prefixes = lastInstructionPrefix.Prefixes};
            return Count - 1;
        }

        public int AppendPrefix(OpCode prefix, ILInstructionParameter parameter)
        {
            var lastInstructionPrefix = instructions[Count - 1] as ILInstructionPrefix;
            if(lastInstructionPrefix != null)
            {
                lastInstructionPrefix.Prefixes.Add(new KeyValuePair<OpCode, ILInstructionParameter>(prefix, parameter));
                return Count - 1;
            }
            instructions.Add(new ILInstructionPrefix {Prefixes = new List<KeyValuePair<OpCode, ILInstructionParameter>> {new KeyValuePair<OpCode, ILInstructionParameter>(prefix, parameter)}});
            return Count++;
        }

        public int BeginExceptionBlock(ILInstructionComment comment)
        {
            instructions.Add(new ILInstruction(InstructionKind.TryStart, default(OpCode), null, comment));
            return Count++;
        }

        public int BeginCatchBlock(TypeILInstructionParameter parameter, ILInstructionComment comment)
        {
            instructions.Add(new ILInstruction(InstructionKind.Catch, default(OpCode), parameter, comment));
            return Count++;
        }

        public int BeginExceptFilterBlock(ILInstructionComment comment)
        {
            instructions.Add(new ILInstruction(InstructionKind.FilteredException, default(OpCode), null, comment));
            return Count++;
        }

        public int BeginFaultBlock(ILInstructionComment comment)
        {
            instructions.Add(new ILInstruction(InstructionKind.Fault, default(OpCode), null, comment));
            return Count++;
        }

        public int BeginFinallyBlock(ILInstructionComment comment)
        {
            instructions.Add(new ILInstruction(InstructionKind.Finally, default(OpCode), null, comment));
            return Count++;
        }

        public int EndExceptionBlock(ILInstructionComment comment)
        {
            instructions.Add(new ILInstruction(InstructionKind.TryEnd, default(OpCode), null, comment));
            return Count++;
        }

        public int WriteLine(StringILInstructionParameter parameter, ILInstructionComment comment)
        {
            instructions.Add(new ILInstruction(InstructionKind.DebugWriteLine, default(OpCode), parameter, comment));
            return Count++;
        }

        public int WriteLine(LocalILInstructionParameter parameter, ILInstructionComment comment)
        {
            instructions.Add(new ILInstruction(InstructionKind.DebugWriteLine, default(OpCode), parameter, comment));
            return Count++;
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

        public KeyValuePair<string, List<KeyValuePair<int, int>>> GetLinesInfo()
        {
            var lines = new List<string>();
            var maxLen = 0;
            foreach(var instruction in instructions)
            {
                var ilInstruction = instruction as ILInstruction;
                if(ilInstruction != null)
                {
                    switch(ilInstruction.Kind)
                    {
                    case InstructionKind.Instruction:
                        var prefixes = (ilInstruction.Prefixes ?? new List<KeyValuePair<OpCode, ILInstructionParameter>>()).Select(pair => pair.Key);
                        lines.Add(margin + string.Join("", prefixes.Concat(new[] {ilInstruction.OpCode}).Select(opcode => opcode.ToString()).ToArray()) + (ilInstruction.Parameter == null ? "" : " " + ilInstruction.Parameter.Format()));
                        break;
                    case InstructionKind.Label:
                        lines.Add((ilInstruction.Parameter == null ? "" : ilInstruction.Parameter.Format()) + ':');
                        break;
                    case InstructionKind.TryStart:
                        lines.Add("TRY");
                        break;
                    case InstructionKind.Catch:
                        lines.Add("CATCH <" + (ilInstruction.Parameter == null ? "Exception" : ilInstruction.Parameter.Format()) + '>');
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
                    case InstructionKind.DebugWriteLine:
                        lines.Add("WriteLine <" + ilInstruction.Parameter.Format() + ">");
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
            var linesInfo = new List<KeyValuePair<int, int>>();
            var currentLine = 1;
            for(var i = 0; i < instructions.Count; ++i)
            {
                var line = lines[i];
                result.Append(line);
                var comment = instructions[i].Comment;
                if(comment != null)
                {
                    if(line.Length <= maxLen)
                    {
                        result.Append(WhiteSpace(maxLen - line.Length));
                        result.Append("// ");
                        result.Append(comment.Format());
                        linesInfo.Add(new KeyValuePair<int, int>(currentLine, currentLine));
                    }
                    else
                    {
                        result.AppendLine();
                        result.Append(WhiteSpace(maxLen));
                        result.Append("// ");
                        result.Append(comment.Format());
                        linesInfo.Add(new KeyValuePair<int, int>(currentLine, currentLine + 1));
                        currentLine++;
                    }
                }
                else linesInfo.Add(new KeyValuePair<int, int>(currentLine, currentLine));
                result.AppendLine();
                currentLine++;
            }
            return new KeyValuePair<string, List<KeyValuePair<int, int>>>(result.ToString(), linesInfo);
        }

        public override string ToString()
        {
            return GetLinesInfo().Key;
        }

        public int Count { get; private set; }

        public enum InstructionKind
        {
            Instruction,
            Label,
            TryStart,
            TryEnd,
            Catch,
            FilteredException,
            Finally,
            Fault,
            DebugWriteLine
        }

        private static void InitMargins(int length)
        {
            if(margins == null)
            {
                margins = new string[length + 1];
                margins[0] = "";
                for(var i = 1; i <= length; ++i)
                    margins[i] = new string(' ', i);
            }
        }

        private static string WhiteSpace(int length)
        {
            return margins[length];
        }

        private const int maxCommentStart = 50;
        private const string margin = "        ";

        private static string[] margins;

        private readonly List<ILInstructionBase> instructions = new List<ILInstructionBase>();

        private readonly Dictionary<Label, int> labelLineNumbers = new Dictionary<Label, int>();

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
            public List<KeyValuePair<OpCode, ILInstructionParameter>> Prefixes { get; set; }
        }

        public class ILInstructionPrefix : ILInstructionBase
        {
            public List<KeyValuePair<OpCode, ILInstructionParameter>> Prefixes { get; set; }
        }
    }
}