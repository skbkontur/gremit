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
    public struct MetadataToken : IEquatable<MetadataToken>
    {
        public MetadataToken(uint token)
        {
            this.token = token;
        }

        public MetadataToken(TokenType type)
            : this(type, 0)
        {
        }

        public MetadataToken(TokenType type, uint rid)
        {
            token = (uint)type | rid;
        }

        public MetadataToken(TokenType type, int rid)
        {
            token = (uint)type | (uint)rid;
        }

        public uint RID => token & 0x00ffffff;

        public TokenType TokenType => (TokenType)(token & 0xff000000);

        public int ToInt32()
        {
            return (int)token;
        }

        public uint ToUInt32()
        {
            return token;
        }

        public override int GetHashCode()
        {
            return (int)token;
        }

        public bool Equals(MetadataToken other)
        {
            return other.token == token;
        }

        public override bool Equals(object obj)
        {
            if(obj is MetadataToken)
            {
                var other = (MetadataToken)obj;
                return other.token == token;
            }

            return false;
        }

        public static bool operator ==(MetadataToken one, MetadataToken other)
        {
            return one.token == other.token;
        }

        public static bool operator !=(MetadataToken one, MetadataToken other)
        {
            return one.token != other.token;
        }

        public override string ToString()
        {
            return $"[{TokenType}:0x{RID.ToString("x4")}]";
        }

        readonly uint token;

        public static readonly MetadataToken Zero = new MetadataToken((uint)0);
    }
}