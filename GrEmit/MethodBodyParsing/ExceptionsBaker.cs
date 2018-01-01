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
    internal sealed class ExceptionsBaker : ByteBuffer
    {
        public ExceptionsBaker(Collection<ExceptionHandler> exceptionHandlers,
                               Collection<Instruction> instructions,
                               Func<OpCode, object, MetadataToken> tokenBuilder)
            : base(0)
        {
            handlers = exceptionHandlers;
            this.instructions = instructions;
            this.tokenBuilder = tokenBuilder;
        }

        public byte[] BakeExceptions()
        {
            if(handlers.IsNullOrEmpty())
                return Empty<byte>.Array;

            WriteExceptions();

            var temp = new byte[length];
            Array.Copy(buffer, temp, length);
            return temp;
        }

        private void WriteExceptions()
        {
            if(handlers.Count < 0x15 && !RequiresFatSection(handlers))
                WriteSmallSection();
            else
                WriteFatSection();
        }

        private static bool RequiresFatSection(Collection<ExceptionHandler> handlers)
        {
            foreach(var handler in handlers)
            {
                if(IsFatRange(handler.TryStart, handler.TryEnd))
                    return true;

                if(IsFatRange(handler.HandlerStart, handler.HandlerEnd))
                    return true;

                if(handler.HandlerType == ExceptionHandlerType.Filter
                   && IsFatRange(handler.FilterStart, handler.HandlerStart))
                    return true;
            }

            return false;
        }

        private static bool IsFatRange(Instruction start, Instruction end)
        {
            if(start == null)
                throw new ArgumentException();

            if(end == null)
                return true;

            return end.Offset - start.Offset > 255 || start.Offset > 65535;
        }

        private void WriteSmallSection()
        {
            const byte eh_table = 0x1;

            WriteByte(eh_table);
            WriteByte((byte)(handlers.Count * 12 + 4));
            WriteBytes(2);

            WriteExceptionHandlers(
                i => WriteUInt16((ushort)i),
                i => WriteByte((byte)i));
        }

        private void WriteFatSection()
        {
            const byte eh_table = 0x1;
            const byte fat_format = 0x40;

            WriteByte(eh_table | fat_format);

            int size = handlers.Count * 24 + 4;
            WriteByte((byte)(size & 0xff));
            WriteByte((byte)((size >> 8) & 0xff));
            WriteByte((byte)((size >> 16) & 0xff));

            WriteExceptionHandlers(WriteInt32, WriteInt32);
        }

        private void WriteExceptionHandlers(Action<int> writeEntry, Action<int> writeLength)
        {
            foreach(var handler in handlers)
            {
                writeEntry((int)handler.HandlerType);

                writeEntry(handler.TryStart.Offset);
                writeLength(GetTargetOffset(handler.TryEnd) - handler.TryStart.Offset);

                writeEntry(handler.HandlerStart.Offset);
                writeLength(GetTargetOffset(handler.HandlerEnd) - handler.HandlerStart.Offset);

                WriteExceptionHandlerSpecific(handler);
            }
        }

        private void WriteExceptionHandlerSpecific(ExceptionHandler handler)
        {
            switch(handler.HandlerType)
            {
            case ExceptionHandlerType.Catch:
                WriteMetadataToken(tokenBuilder(default(OpCode), handler.CatchType));
                break;
            case ExceptionHandlerType.Filter:
                WriteInt32(handler.FilterStart.Offset);
                break;
            default:
                WriteInt32(0);
                break;
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
        private readonly Collection<ExceptionHandler> handlers;
    }
}