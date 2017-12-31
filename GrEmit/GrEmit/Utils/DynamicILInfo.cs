using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection.Emit;

namespace GrEmit.Utils
{
    public class DynamicILInfo
    {
        #region Private Data Members 
        private DynamicMethod m_method;
        private DynamicScope m_scope;
        private byte[] m_exceptions;
        private byte[] m_code;
        private byte[] m_localSignature;
        private int m_maxStackSize;
        private int m_methodSignature;
        #endregion

        #region Constructor
        internal DynamicILInfo(DynamicScope scope, DynamicMethod method, byte[] methodSignature)
        {
            m_method = method;
            m_scope = scope;
            m_methodSignature = m_scope.GetTokenFor(methodSignature);
            m_exceptions = new byte[0];
            m_code = new byte[0];
            m_localSignature = new byte[0];
        }
        #endregion

        #region Public ILGenerator Methods 
        internal DynamicScope DynamicScope { get { return m_scope; } }

        public void SetCode(byte[] code, int maxStackSize)
        {
            if (code == null)
                code = new byte[0];

            m_code = (byte[])code.Clone();
            m_maxStackSize = maxStackSize;
        }

        public void SetExceptions(byte[] exceptions)
        {
            if (exceptions == null)
                exceptions = new byte[0];

            m_exceptions = (byte[])exceptions.Clone();
        }

        public void SetLocalSignature(byte[] localSignature)
        {
            if (localSignature == null)
                localSignature = new byte[0];

            m_localSignature = (byte[])localSignature.Clone();
        }
        #endregion

        #region Public Scope Methods
        public int GetTokenFor(RuntimeMethodHandle method)
        {
            return DynamicScope.GetTokenFor(method);
        }
        public int GetTokenFor(RuntimeMethodHandle method, RuntimeTypeHandle contextType)
        {
            return DynamicScope.GetTokenFor(method, contextType);
        }
        public int GetTokenFor(RuntimeFieldHandle field)
        {
            return DynamicScope.GetTokenFor(field);
        }
        public int GetTokenFor(RuntimeFieldHandle field, RuntimeTypeHandle contextType)
        {
            return DynamicScope.GetTokenFor(field, contextType);
        }
        public int GetTokenFor(RuntimeTypeHandle type)
        {
            return DynamicScope.GetTokenFor(type);
        }
        public int GetTokenFor(string literal)
        {
            return DynamicScope.GetTokenFor(literal);
        }
        public int GetTokenFor(byte[] signature)
        {
            return DynamicScope.GetTokenFor(signature);
        }
        #endregion
    }

    internal class DynamicScope
    {
        #region Private Data Members
        internal List<object> m_tokens;
        #endregion

        #region Constructor
        internal DynamicScope()
        {
            m_tokens = new List<object>();
            m_tokens.Add(null);
        }
        #endregion

        #region Public Methods
        public int GetTokenFor(RuntimeMethodHandle method)
        {
            //IRuntimeMethodInfo methodReal = method.GetMethodInfo();
            //RuntimeMethodHandleInternal rmhi = methodReal.Value;

            //if (methodReal != null && !RuntimeMethodHandle.IsDynamicMethod(rmhi))
            //{
            //    RuntimeType type = RuntimeMethodHandle.GetDeclaringType(rmhi);
            //    if ((type != null) && RuntimeTypeHandle.IsGenericType(type))
            //    {
            //        // Do we really need to retrieve this much info just to throw an exception? 
            //        MethodBase m = RuntimeType.GetMethodBase(methodReal);
            //        Type t = m.DeclaringType.GetGenericTypeDefinition();

            //        throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, "Argument_MethodDeclaringTypeGenericLcg", m, t));
            //    }
            //}

            m_tokens.Add(method);
            return m_tokens.Count - 1 | (int)MetadataTokenType.MethodDef;
        }
        public int GetTokenFor(RuntimeMethodHandle method, RuntimeTypeHandle typeContext)
        {
            m_tokens.Add(new GenericMethodInfo(method, typeContext));
            return m_tokens.Count - 1 | (int)MetadataTokenType.MethodDef;
        }
        public int GetTokenFor(RuntimeFieldHandle field)
        {
            m_tokens.Add(field);
            return m_tokens.Count - 1 | (int)MetadataTokenType.FieldDef;
        }
        public int GetTokenFor(RuntimeFieldHandle field, RuntimeTypeHandle typeContext)
        {
            m_tokens.Add(new GenericFieldInfo(field, typeContext));
            return m_tokens.Count - 1 | (int)MetadataTokenType.FieldDef;
        }
        public int GetTokenFor(RuntimeTypeHandle type)
        {
            m_tokens.Add(type);
            return m_tokens.Count - 1 | (int)MetadataTokenType.TypeDef;
        }
        public int GetTokenFor(string literal)
        {
            m_tokens.Add(literal);
            return m_tokens.Count - 1 | (int)MetadataTokenType.String;
        }
        public int GetTokenFor(byte[] signature)
        {
            m_tokens.Add(signature);
            return m_tokens.Count - 1 | (int)MetadataTokenType.Signature;
        }
        #endregion
    }

    internal sealed class GenericMethodInfo
    {
        internal RuntimeMethodHandle m_methodHandle;
        internal RuntimeTypeHandle m_context;
        internal GenericMethodInfo(RuntimeMethodHandle methodHandle, RuntimeTypeHandle context)
        {
            m_methodHandle = methodHandle;
            m_context = context;
        }
    }

    internal sealed class GenericFieldInfo
    {
        internal RuntimeFieldHandle m_fieldHandle;
        internal RuntimeTypeHandle m_context;
        internal GenericFieldInfo(RuntimeFieldHandle fieldHandle, RuntimeTypeHandle context)
        {
            m_fieldHandle = fieldHandle;
            m_context = context;
        }
    }

    internal enum MetadataTokenType
    {
        Module = 0x00000000,
        TypeRef = 0x01000000,
        TypeDef = 0x02000000,
        FieldDef = 0x04000000,
        MethodDef = 0x06000000,
        ParamDef = 0x08000000,
        InterfaceImpl = 0x09000000,
        MemberRef = 0x0a000000,
        CustomAttribute = 0x0c000000,
        Permission = 0x0e000000,
        Signature = 0x11000000,
        Event = 0x14000000,
        Property = 0x17000000,
        ModuleRef = 0x1a000000,
        TypeSpec = 0x1b000000,
        Assembly = 0x20000000,
        AssemblyRef = 0x23000000,
        File = 0x26000000,
        ExportedType = 0x27000000,
        ManifestResource = 0x28000000,
        GenericPar = 0x2a000000,
        MethodSpec = 0x2b000000,
        String = 0x70000000,
        Name = 0x71000000,
        BaseType = 0x72000000,
        Invalid = 0x7FFFFFFF,
    }
}
