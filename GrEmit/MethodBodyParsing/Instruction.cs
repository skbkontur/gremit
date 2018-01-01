//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
// Copyright (c) 2016 Igor Chevdar
//
// Licensed under the MIT/X11 license.
//

using System;
using System.Reflection;
using System.Text;

namespace GrEmit.MethodBodyParsing
{
    public sealed class Instruction
    {
        internal Instruction(int offset, OpCode opCode)
        {
            Offset = offset;
            OpCode = opCode;
        }

        internal Instruction(OpCode opcode, object operand)
        {
            OpCode = opcode;
            Operand = operand;
        }

        public int Offset { get; set; }

        public OpCode OpCode { get; set; }

        public object Operand { get; set; }

        public Instruction Previous { get; set; }

        public Instruction Next { get; set; }

        public int GetSize()
        {
            int size = OpCode.Size;

            switch(OpCode.OperandType)
            {
            case OperandType.InlineSwitch:
                return size + (1 + ((Instruction[])Operand).Length) * 4;
            case OperandType.InlineI8:
            case OperandType.InlineR:
                return size + 8;
            case OperandType.InlineBrTarget:
            case OperandType.InlineField:
            case OperandType.InlineI:
            case OperandType.InlineMethod:
            case OperandType.InlineString:
            case OperandType.InlineTok:
            case OperandType.InlineType:
            case OperandType.ShortInlineR:
            case OperandType.InlineSig:
                return size + 4;
            case OperandType.InlineArg:
            case OperandType.InlineVar:
                return size + 2;
            case OperandType.ShortInlineBrTarget:
            case OperandType.ShortInlineI:
            case OperandType.ShortInlineArg:
            case OperandType.ShortInlineVar:
                return size + 1;
            default:
                return size;
            }
        }

        public override string ToString()
        {
            var instruction = new StringBuilder();

            AppendLabel(instruction, this);
            instruction.Append(':');
            instruction.Append(' ');
            instruction.Append(OpCode.Name);

            if(Operand == null)
                return instruction.ToString();

            instruction.Append(' ');

            switch(OpCode.OperandType)
            {
            case OperandType.ShortInlineBrTarget:
            case OperandType.InlineBrTarget:
                AppendLabel(instruction, (Instruction)Operand);
                break;
            case OperandType.InlineSwitch:
                var labels = (Instruction[])Operand;
                for(int i = 0; i < labels.Length; i++)
                {
                    if(i > 0)
                        instruction.Append(',');

                    AppendLabel(instruction, labels[i]);
                }
                break;
            case OperandType.InlineString:
                instruction.Append('\"');
                instruction.Append(Operand);
                instruction.Append('\"');
                break;
            default:
                instruction.Append(Operand);
                break;
            }

            return instruction.ToString();
        }

        private static void AppendLabel(StringBuilder builder, Instruction instruction)
        {
            builder.Append("IL_");
            builder.Append(instruction.Offset.ToString("D4"));
        }

        public static Instruction Create(OpCode opcode)
        {
            if(opcode.OperandType != OperandType.InlineNone)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, null);
        }

        public static Instruction Create(OpCode opcode, MetadataToken value)
        {
            if(value == MetadataToken.Zero)
                throw new ArgumentNullException(nameof(value));

            return new Instruction(opcode, value);
        }

        public static Instruction Create(OpCode opcode, Type type)
        {
            if(type == null)
                throw new ArgumentNullException(nameof(type));
            if(opcode.OperandType != OperandType.InlineType
               && opcode.OperandType != OperandType.InlineTok)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, type);
        }

        public static Instruction Create(OpCode opcode, byte[] site)
        {
            if(site == null)
                throw new ArgumentNullException(nameof(site));
            if(opcode.Code != Code.Calli)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, site);
        }

        public static Instruction Create(OpCode opcode, MethodBase method)
        {
            if(method == null)
                throw new ArgumentNullException(nameof(method));
            if(opcode.OperandType != OperandType.InlineMethod
               && opcode.OperandType != OperandType.InlineTok)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, method);
        }

        public static Instruction Create(OpCode opcode, FieldInfo field)
        {
            if(field == null)
                throw new ArgumentNullException(nameof(field));
            if(opcode.OperandType != OperandType.InlineField
               && opcode.OperandType != OperandType.InlineTok)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, field);
        }

        public static Instruction Create(OpCode opcode, string value)
        {
            if(value == null)
                throw new ArgumentNullException(nameof(value));
            if(opcode.OperandType != OperandType.InlineString)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, value);
        }

        public static Instruction Create(OpCode opcode, sbyte value)
        {
            if(opcode.OperandType != OperandType.ShortInlineI
               && opcode != OpCodes.Ldc_I4_S)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, value);
        }

        public static Instruction Create(OpCode opcode, byte value)
        {
            if(opcode.OperandType != OperandType.ShortInlineI ||
               opcode == OpCodes.Ldc_I4_S)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, value);
        }

        public static Instruction Create(OpCode opcode, int value)
        {
            if(opcode.OperandType != OperandType.InlineI
               && opcode.OperandType != OperandType.InlineVar
               && opcode.OperandType != OperandType.ShortInlineVar
               && opcode.OperandType != OperandType.InlineArg
               && opcode.OperandType != OperandType.ShortInlineArg)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, value);
        }

        public static Instruction Create(OpCode opcode, long value)
        {
            if(opcode.OperandType != OperandType.InlineI8)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, value);
        }

        public static Instruction Create(OpCode opcode, float value)
        {
            if(opcode.OperandType != OperandType.ShortInlineR)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, value);
        }

        public static Instruction Create(OpCode opcode, double value)
        {
            if(opcode.OperandType != OperandType.InlineR)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, value);
        }

        public static Instruction Create(OpCode opcode, Instruction target)
        {
            if(target == null)
                throw new ArgumentNullException(nameof(target));
            if(opcode.OperandType != OperandType.InlineBrTarget &&
               opcode.OperandType != OperandType.ShortInlineBrTarget)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, target);
        }

        public static Instruction Create(OpCode opcode, Instruction[] targets)
        {
            if(targets == null)
                throw new ArgumentNullException(nameof(targets));
            if(opcode.OperandType != OperandType.InlineSwitch)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, targets);
        }
    }
}