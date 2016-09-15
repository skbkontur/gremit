using System;

namespace GrEmit.MethodBodyParsing
{
    public class LocalInfo
    {
        public LocalInfo(byte[] signature)
        {
            Signature = signature;
        }

        public LocalInfo(Type localType, bool isPinned)
        {
            LocalType = localType;
            IsPinned = isPinned;
        }

        public int LocalIndex = -1;
        public byte[] Signature;
        public Type LocalType;
        public bool IsPinned;
    }
}