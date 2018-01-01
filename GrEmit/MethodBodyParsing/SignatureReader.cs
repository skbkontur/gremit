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
    public class ParsedMethodSignature
    {
        public bool HasReturnType => !(ReturnTypeSignature.Length == 1 && ReturnTypeSignature[0] == (byte)ElementType.Void);
        public byte CallingConvention;
        public bool HasThis;
        public bool ExplicitThis;
        public byte[] ReturnTypeSignature;
        public int ParamCount;
    }

    public class SignatureReader : ByteBuffer
    {
        public SignatureReader(byte[] signature)
            : base(signature)
        {
        }

        internal LocalInfoCollection ReadLocalVarSig()
        {
            const byte local_sig = 0x7;

            if(ReadByte() != local_sig)
                throw new NotSupportedException();

            var localVariables = new LocalInfoCollection();

            var count = ReadCompressedUInt32();

            for(int i = 0; i < count; ++i)
                localVariables.Add(new LocalInfo(ReadTypeSignature()));

            return localVariables;
        }

        public ParsedMethodSignature ReadAndParseMethodSignature()
        {
            var method = new ParsedMethodSignature();

            var calling_convention = ReadByte();

            const byte has_this = 0x20;
            const byte explicit_this = 0x40;

            if((calling_convention & has_this) != 0)
            {
                method.HasThis = true;
                calling_convention = (byte)(calling_convention & ~has_this);
            }

            if((calling_convention & explicit_this) != 0)
            {
                method.ExplicitThis = true;
                calling_convention = (byte)(calling_convention & ~explicit_this);
            }

            method.CallingConvention = calling_convention;

            if((calling_convention & 0x10) != 0)
            {
                // arity
                ReadCompressedUInt32();
            }

            var param_count = ReadCompressedUInt32();
            method.ParamCount = (int)param_count;

            while(buffer[position] == (byte)ElementType.CModOpt
                  || buffer[position] == (byte)ElementType.CModReqD)
            {
                ReadByte();
                ReadTypeTokenSignature();
            }

            method.ReturnTypeSignature = ReadTypeSignature();

            if(param_count == 0)
                return method;

            for(int i = 0; i < param_count; i++)
                ReadTypeSignature();

            return method;
        }

        private byte[] ReadTypeSignature()
        {
            var writer = new ByteBuffer();

            var elementType = ReadByte();
            writer.WriteByte(elementType);

            writer.WriteBytes(ReadTypeSignature((ElementType)elementType));

            writer.position = 0;
            return writer.ReadBytes(writer.length);
        }

        private byte[] ReadTypeTokenSignature()
        {
            var writer = new ByteBuffer();

            writer.WriteCompressedUInt32(ReadCompressedUInt32());

            writer.position = 0;
            return writer.ReadBytes(writer.length);
        }

        private byte[] ReadMethodSignature()
        {
            var writer = new ByteBuffer();

            var calling_convention = ReadByte();
            writer.WriteByte(calling_convention);

            if((calling_convention & 0x10) != 0)
            {
                // arity
                writer.WriteCompressedUInt32(ReadCompressedUInt32());
            }

            var param_count = ReadCompressedUInt32();
            writer.WriteCompressedUInt32(param_count);

            // return type
            writer.WriteBytes(ReadTypeSignature());

            if(param_count == 0)
            {
                writer.position = 0;
                return writer.ReadBytes(writer.length);
            }

            for(int i = 0; i < param_count; i++)
                writer.WriteBytes(ReadTypeSignature());

            writer.position = 0;
            return writer.ReadBytes(writer.length);
        }

        private byte[] ReadTypeSignature(ElementType etype)
        {
            var writer = new ByteBuffer();

            switch(etype)
            {
            case ElementType.ValueType:
                writer.WriteBytes(ReadTypeTokenSignature());
                break;
            case ElementType.Class:
                writer.WriteBytes(ReadTypeTokenSignature());
                break;
            case ElementType.Ptr:
                writer.WriteBytes(ReadTypeSignature());
                break;
            case ElementType.FnPtr:
                writer.WriteBytes(ReadMethodSignature());
                break;
            case ElementType.ByRef:
                writer.WriteBytes(ReadTypeSignature());
                break;
            case ElementType.Pinned:
                writer.WriteBytes(ReadTypeSignature());
                break;
            case ElementType.SzArray:
                writer.WriteBytes(ReadTypeSignature());
                break;
            case ElementType.Array:
                writer.WriteBytes(ReadArrayTypeSignature());
                break;
            case ElementType.CModOpt:
                writer.WriteBytes(ReadTypeTokenSignature());
                writer.WriteBytes(ReadTypeSignature());
                break;
            case ElementType.CModReqD:
                writer.WriteBytes(ReadTypeTokenSignature());
                writer.WriteBytes(ReadTypeSignature());
                break;
            case ElementType.Sentinel:
                writer.WriteBytes(ReadTypeSignature());
                break;
            case ElementType.Var:
                writer.WriteCompressedUInt32(ReadCompressedUInt32());
                break;
            case ElementType.MVar:
                writer.WriteCompressedUInt32(ReadCompressedUInt32());
                break;
            case ElementType.GenericInst:
                {
                    // attrs
                    var readByte = ReadByte();
                    writer.WriteByte(readByte);
                    // element_type
                    if((ElementType)readByte == ElementType.Internal)
                        writer.WriteBytes(ReadBytes(IntPtr.Size));
                    else
                        writer.WriteBytes(ReadTypeTokenSignature());

                    writer.WriteBytes(ReadGenericInstanceSignature());
                    break;
                }
            case ElementType.Internal:
                writer.WriteBytes(ReadBytes(IntPtr.Size));
                break;
            default:
                writer.WriteBytes(ReadBuiltInType());
                break;
            }
            writer.position = 0;
            return writer.ReadBytes(writer.length);
        }

        private byte[] ReadGenericInstanceSignature()
        {
            var writer = new ByteBuffer();

            var arity = ReadCompressedUInt32();
            writer.WriteCompressedUInt32(arity);

            for(int i = 0; i < arity; i++)
                writer.WriteBytes(ReadTypeSignature());

            writer.position = 0;
            return writer.ReadBytes(writer.length);
        }

        private byte[] ReadArrayTypeSignature()
        {
            var writer = new ByteBuffer();

            // element_type
            writer.WriteBytes(ReadTypeSignature());

            // rank
            writer.WriteCompressedUInt32(ReadCompressedUInt32());

            var sizes = ReadCompressedUInt32();
            writer.WriteCompressedUInt32(sizes);

            for(int i = 0; i < sizes; i++)
                writer.WriteCompressedUInt32(ReadCompressedUInt32());

            var low_bounds = ReadCompressedUInt32();
            writer.WriteCompressedUInt32(low_bounds);

            for(int i = 0; i < low_bounds; i++)
                writer.WriteCompressedInt32(ReadCompressedInt32());

            writer.position = 0;
            return writer.ReadBytes(writer.length);
        }

        private byte[] ReadBuiltInType()
        {
            return new byte[0];
        }
    }
}