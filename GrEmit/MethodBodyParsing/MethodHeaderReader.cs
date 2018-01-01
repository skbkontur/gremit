using System;

namespace GrEmit.MethodBodyParsing
{
    internal sealed unsafe class MethodHeaderReader : UnmanagedByteBuffer
    {
        public MethodHeaderReader(byte* buffer)
            : base(buffer)
        {
        }

        public MethodHeader Read()
        {
            position = 0;
            var header = new MethodHeader();

            var flags = ReadByte();
            switch(flags & 0x3)
            {
            case CorILMethod_TinyFormat:
                header.CodeSize = flags >> 2;
                header.MaxStack = 8;
                break;
            case CorILMethod_FatFormat:
                var hi = ReadByte();
                header.MaxStack = ReadUInt16();
                header.CodeSize = (int)ReadUInt32();
                header.LocalVarToken = new MetadataToken(ReadUInt32());
                header.InitLocals = (flags & CorILMethod_InitLocals) != 0;
                header.HasExceptions = (flags & CorILMethod_MoreSects) != 0;
                break;
            default:
                throw new InvalidOperationException();
            }
            header.HeaderSize = position;

            return header;
        }

        public const byte CorILMethod_TinyFormat = 0x2;
        public const byte CorILMethod_FatFormat = 0x3;
        public const byte CorILMethod_InitLocals = 0x10;
        public const byte CorILMethod_MoreSects = 0x8;
    }
}