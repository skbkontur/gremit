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
    internal sealed class FullMethodBodyBaker : ByteBuffer
    {
        public FullMethodBodyBaker(MethodBody body, Func<OpCode, object, MetadataToken> tokenBuilder)
            : base(0)
        {
            this.body = body;
            this.tokenBuilder = (opCode, operand) =>
                {
                    if(operand is MetadataToken)
                        return (MetadataToken)operand;
                    if(tokenBuilder == null)
                        throw new InvalidOperationException($"Operand {operand} is not resolved to metadata token");
                    return tokenBuilder(opCode, operand);
                };
        }

        public byte[] BakeMethodBody()
        {
            WriteMethodBody();

            var temp = new byte[length];
            Array.Copy(buffer, temp, length);
            return temp;
        }

        private void WriteMethodBody()
        {
            var ilCode = new ILCodeBaker(body.Instructions, tokenBuilder).BakeILCode();
            codeSize = ilCode.Length;

            if(RequiresFatHeader())
                WriteFatHeader();
            else
                WriteByte((byte)(0x2 | (codeSize << 2))); // tiny

            WriteBytes(ilCode);

            if(body.HasExceptionHandlers)
            {
                Align(4);
                WriteBytes(new ExceptionsBaker(body.ExceptionHandlers, body.Instructions, tokenBuilder).BakeExceptions());
            }
        }

        private void WriteFatHeader()
        {
            byte flags = 0x3; // fat
            if(body.InitLocals)
                flags |= 0x10; // init locals
            if(body.HasExceptionHandlers)
                flags |= 0x8; // more sections

            WriteByte(flags);
            WriteByte(0x30);
            WriteInt16((short)body.MaxStack);
            WriteInt32(codeSize);
            WriteMetadataToken(body.LocalVarToken);
        }

        private bool RequiresFatHeader()
        {
            return codeSize >= 64
                   || body.InitLocals
                   || body.LocalVariablesCount() > 0
                   || body.HasExceptionHandlers
                   || body.MaxStack > 8;
        }

        private void WriteMetadataToken(MetadataToken token)
        {
            WriteUInt32(token.ToUInt32());
        }

        private void Align(int align)
        {
            align--;
            WriteBytes(((position + align) & ~align) - position);
        }

        private readonly MethodBody body;
        private readonly Func<OpCode, object, MetadataToken> tokenBuilder;
        private int codeSize;
    }
}