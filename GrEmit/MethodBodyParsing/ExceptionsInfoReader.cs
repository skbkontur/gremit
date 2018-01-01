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
    internal sealed unsafe class ExceptionsInfoReader : UnmanagedByteBuffer
    {
        public ExceptionsInfoReader(byte* buffer, Func<MetadataToken, object> tokenResolver, bool resolveTokens)
            : base(buffer)
        {
            this.tokenResolver = tokenResolver;
            this.resolveTokens = resolveTokens;
        }

        public static void Read(byte[] buffer, Func<MetadataToken, object> tokenResolver, bool resolveTokens, MethodBody body)
        {
            if(buffer != null && buffer.Length > 0)
            {
                fixed(byte* b = &buffer[0])
                    new ExceptionsInfoReader(b, tokenResolver, resolveTokens).Read(body);
            }
        }

        public void Read(MethodBody body)
        {
            this.body = body;
            ReadSection();
        }

        private void ReadSection()
        {
            position = 0;
            const byte fat_format = 0x40;
            const byte more_sects = 0x80;

            var flags = ReadByte();
            if((flags & fat_format) == 0)
                ReadSmallSection();
            else
                ReadFatSection();

            if((flags & more_sects) != 0)
                ReadSection();
        }

        private void ReadSmallSection()
        {
            var count = ReadByte() / 12;
            Advance(2);

            ReadExceptionHandlers(
                count,
                () => (int)ReadUInt16(),
                () => (int)ReadByte());
        }

        private void ReadFatSection()
        {
            position--;
            var count = (ReadInt32() >> 8) / 24;

            ReadExceptionHandlers(
                count,
                ReadInt32,
                ReadInt32);
        }

        private void ReadExceptionHandlers(int count, Func<int> read_entry, Func<int> read_length)
        {
            for(int i = 0; i < count; i++)
            {
                var handler = new ExceptionHandler(
                    (ExceptionHandlerType)(read_entry() & 0x7));

                handler.TryStart = GetInstruction(read_entry());
                handler.TryEnd = GetInstruction(handler.TryStart.Offset + read_length());

                handler.HandlerStart = GetInstruction(read_entry());
                handler.HandlerEnd = GetInstruction(handler.HandlerStart.Offset + read_length());

                ReadExceptionHandlerSpecific(handler);

                body.ExceptionHandlers.Add(handler);
            }
        }

        private void ReadExceptionHandlerSpecific(ExceptionHandler handler)
        {
            switch(handler.HandlerType)
            {
            case ExceptionHandlerType.Catch:
                handler.CatchType = ReadToken();
                break;
            case ExceptionHandlerType.Filter:
                handler.FilterStart = GetInstruction(ReadInt32());
                break;
            default:
                Advance(4);
                break;
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

        private readonly Func<MetadataToken, object> tokenResolver;
        private readonly bool resolveTokens;

        private MethodBody body;
    }
}