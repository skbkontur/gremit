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
    internal sealed unsafe class ILCodeReader : UnmanagedByteBuffer
    {
        public ILCodeReader(byte* buffer, int codeSize, Func<MetadataToken, object> tokenResolver, bool resolveTokens)
            : base(buffer)
        {
            this.codeSize = codeSize;
            this.tokenResolver = tokenResolver;
            this.resolveTokens = resolveTokens;
        }

        public static void Read(byte[] buffer, Func<MetadataToken, object> tokenResolver, bool resolveTokens, MethodBody body)
        {
            fixed(byte* b = &buffer[0])
                new ILCodeReader(b, buffer.Length, tokenResolver, resolveTokens).Read(body);
        }

        public void Read(MethodBody body)
        {
            this.body = body;
            ReadCode();
        }

        private void ReadCode()
        {
            position = 0;
            var end = codeSize;
            var instructions = body.Instructions;

            while(position < end)
            {
                var offset = position;
                var opcode = ReadOpCode();
                var current = new Instruction(offset, opcode);

                if(opcode.OperandType != OperandType.InlineNone)
                    current.Operand = ReadOperand(current);

                instructions.Add(current);
            }

            ResolveBranches(instructions);
        }

        private OpCode ReadOpCode()
        {
            var il_opcode = ReadByte();
            return il_opcode != 0xfe
                       ? OpCodes.OneByteOpCode[il_opcode]
                       : OpCodes.TwoBytesOpCode[ReadByte()];
        }

        private object ReadOperand(Instruction instruction)
        {
            switch(instruction.OpCode.OperandType)
            {
            case OperandType.InlineSwitch:
                var length = ReadInt32();
                var base_offset = position + (4 * length);
                var branches = new int[length];
                for(int i = 0; i < length; i++)
                    branches[i] = base_offset + ReadInt32();
                return branches;
            case OperandType.ShortInlineBrTarget:
                return ReadSByte() + position;
            case OperandType.InlineBrTarget:
                return ReadInt32() + position;
            case OperandType.ShortInlineI:
                if(instruction.OpCode == OpCodes.Ldc_I4_S)
                    return ReadSByte();

                return ReadByte();
            case OperandType.InlineI:
                return ReadInt32();
            case OperandType.ShortInlineR:
                return ReadSingle();
            case OperandType.InlineR:
                return ReadDouble();
            case OperandType.InlineI8:
                return ReadInt64();
            case OperandType.ShortInlineVar:
                return (int)ReadByte();
            case OperandType.InlineVar:
                return (int)ReadUInt16();
            case OperandType.ShortInlineArg:
                return (int)ReadByte();
            case OperandType.InlineArg:
                return (int)ReadUInt16();
            case OperandType.InlineSig:
                return ReadToken();
            case OperandType.InlineString:
                return ReadToken();
            case OperandType.InlineTok:
            case OperandType.InlineType:
            case OperandType.InlineMethod:
            case OperandType.InlineField:
                return ReadToken();
            default:
                throw new NotSupportedException();
            }
        }

        private void ResolveBranches(Collection<Instruction> instructions)
        {
            var items = instructions.items;
            var size = instructions.size;

            for(int i = 0; i < size; i++)
            {
                var instruction = items[i];
                switch(instruction.OpCode.OperandType)
                {
                case OperandType.ShortInlineBrTarget:
                case OperandType.InlineBrTarget:
                    instruction.Operand = GetInstruction((int)instruction.Operand);
                    break;
                case OperandType.InlineSwitch:
                    var offsets = (int[])instruction.Operand;
                    var branches = new Instruction[offsets.Length];
                    for(int j = 0; j < offsets.Length; j++)
                        branches[j] = GetInstruction(offsets[j]);

                    instruction.Operand = branches;
                    break;
                }
            }
        }

        private Instruction GetInstruction(int offset)
        {
            return body.Instructions.GetInstruction(offset);
        }

        private object ReadToken()
        {
            var token = new MetadataToken(ReadUInt32());
            return resolveTokens ? tokenResolver(token) : token;
        }

        private readonly int codeSize;
        private readonly Func<MetadataToken, object> tokenResolver;
        private readonly bool resolveTokens;
        private MethodBody body;
    }
}