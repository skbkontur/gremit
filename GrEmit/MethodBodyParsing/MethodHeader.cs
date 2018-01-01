namespace GrEmit.MethodBodyParsing
{
    internal class MethodHeader
    {
        public int HeaderSize { get; set; }
        public int CodeSize { get; set; }
        public int MaxStack { get; set; }
        public bool InitLocals { get; set; }
        public MetadataToken LocalVarToken { get; set; }
        public bool HasExceptions { get; set; }
    }
}