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

namespace GrEmit.MethodBodyParsing
{
    internal sealed class ILCodeBaker : ByteBuffer
    {
        public ILCodeBaker(Collection<Instruction> instructions, Func<OpCode, object, MetadataToken> tokenBuilder)
            : base(0)
        {
            this.instructions = instructions;
            this.tokenBuilder = tokenBuilder;
        }

        public byte[] BakeILCode()
        {
            WriteInstructions();

            var temp = new byte[length];
            Array.Copy(buffer, temp, length);
            return temp;
        }

        private void WriteInstructions()
        {
            foreach(var instruction in instructions)
            {
                WriteOpCode(instruction.OpCode);
                WriteOperand(instruction);
            }
        }

        private void WriteOpCode(OpCode opcode)
        {
            if(opcode.Size == 1)
                WriteByte(opcode.Op2);
            else
            {
                WriteByte(opcode.Op1);
                WriteByte(opcode.Op2);
            }
        }

        private void WriteOperand(Instruction instruction)
        {
            var opcode = instruction.OpCode;
            var operandType = opcode.OperandType;
            if(operandType == OperandType.InlineNone)
                return;

            var operand = instruction.Operand;
            if(operand == null)
                throw new ArgumentException();

            switch(operandType)
            {
            case OperandType.InlineSwitch:
                {
                    var targets = (Instruction[])operand;
                    WriteInt32(targets.Length);
                    var diff = instruction.Offset + opcode.Size + (4 * (targets.Length + 1));
                    foreach(var target in targets)
                        WriteInt32(GetTargetOffset(target) - diff);
                    break;
                }
            case OperandType.ShortInlineBrTarget:
                {
                    var target = (Instruction)operand;
                    WriteSByte((sbyte)(GetTargetOffset(target) - (instruction.Offset + opcode.Size + 1)));
                    break;
                }
            case OperandType.InlineBrTarget:
                {
                    var target = (Instruction)operand;
                    WriteInt32(GetTargetOffset(target) - (instruction.Offset + opcode.Size + 4));
                    break;
                }
            case OperandType.ShortInlineVar:
                WriteByte((byte)(int)operand);
                break;
            case OperandType.ShortInlineArg:
                WriteByte((byte)(int)operand);
                break;
            case OperandType.InlineVar:
                WriteInt16((short)(int)operand);
                break;
            case OperandType.InlineArg:
                WriteInt16((short)(int)operand);
                break;
            case OperandType.InlineSig:
                WriteMetadataToken(tokenBuilder(opcode, operand));
                break;
            case OperandType.ShortInlineI:
                if(opcode == OpCodes.Ldc_I4_S)
                    WriteSByte((sbyte)operand);
                else
                    WriteByte((byte)operand);
                break;
            case OperandType.InlineI:
                WriteInt32((int)operand);
                break;
            case OperandType.InlineI8:
                WriteInt64((long)operand);
                break;
            case OperandType.ShortInlineR:
                WriteSingle((float)operand);
                break;
            case OperandType.InlineR:
                WriteDouble((double)operand);
                break;
            case OperandType.InlineString:
                WriteMetadataToken(tokenBuilder(opcode, operand));
                break;
            case OperandType.InlineType:
            case OperandType.InlineField:
            case OperandType.InlineMethod:
            case OperandType.InlineTok:
                WriteMetadataToken(tokenBuilder(opcode, operand));
                break;
            default:
                throw new ArgumentException();
            }
        }

        private int GetTargetOffset(Instruction instruction)
        {
            if(instruction == null)
            {
                var last = instructions[instructions.size - 1];
                return last.Offset + last.GetSize();
            }

            return instruction.Offset;
        }

        private void WriteMetadataToken(MetadataToken token)
        {
            WriteUInt32(token.ToUInt32());
        }

        private readonly Collection<Instruction> instructions;
        private readonly Func<OpCode, object, MetadataToken> tokenBuilder;
    }
}