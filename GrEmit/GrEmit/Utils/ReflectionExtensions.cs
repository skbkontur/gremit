using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace GrEmit.Utils
{
    public static class ReflectionExtensions
    {
        static ReflectionExtensions()
        {
            isMono = Type.GetType("Mono.Runtime") != null;

            var assembly = typeof(MethodInfo).Assembly;
            var types = assembly.GetTypes();

            runtimeTypeType = FindType(types, isMono ? "MonoType" : "RuntimeType");
            var byRefTypeType = FindType(types, isMono ? "ByRefType" : "SymbolType");
            var pointerTypeType = FindType(types, isMono ? "PointerType" : "SymbolType");
            var arrayTypeType = FindType(types, isMono ? "ArrayType" : "SymbolType");

            typeBuilderInstType = FindType(types, isMono ? "MonoGenericClass" : "TypeBuilderInstantiation");

            runtimeMethodInfoType = FindType(types, isMono ? "MonoMethod" : "RuntimeMethodInfo");
            runtimeGenericMethodInfoType = FindType(types, isMono ? "MonoGenericMethod" : "RuntimeMethodInfo");
            runtimeConstructorInfoType = FindType(types, isMono ? "MonoCMethod" : "RuntimeConstructorInfo");
            methodOnTypeBuilderInstType = FindType(types, isMono ? "MethodOnTypeBuilderInst" : "MethodOnTypeBuilderInstantiation");
            //constructorOnTypeBuilderInstType = FindType(types, "ConstructorOnTypeBuilderInstantiation");
            if(!isMono)
                methodBuilderInstType = FindType(types, "MethodBuilderInstantiation");

            parameterTypesExtractors = new Hashtable();
            returnTypeExtractors = new Hashtable();
            baseTypeOfTypeExtractors = new Hashtable();
            interfacesOfTypeExtractors = new Hashtable();
            typeComparers = new Hashtable();
            hashCodeCalculators = new Hashtable();
            assignabilityCheckers = new Hashtable();
            parameterTypesExtractors[runtimeMethodInfoType]
                = parameterTypesExtractors[runtimeGenericMethodInfoType]
                  = parameterTypesExtractors[typeof(DynamicMethod)]
                    = parameterTypesExtractors[runtimeConstructorInfoType] =
                      (Func<MethodBase, Type[]>)(method => method.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
            returnTypeExtractors[runtimeMethodInfoType]
                = returnTypeExtractors[runtimeGenericMethodInfoType]
                  = returnTypeExtractors[typeof(DynamicMethod)]
                    = returnTypeExtractors[typeof(MethodBuilder)] =
                      (Func<MethodInfo, Type>)(method => method.ReturnType);
            baseTypeOfTypeExtractors[runtimeTypeType]
                = baseTypeOfTypeExtractors[typeof(TypeBuilder)]
                  = baseTypeOfTypeExtractors[typeof(GenericTypeParameterBuilder)]
                    = baseTypeOfTypeExtractors[byRefTypeType]
                      = baseTypeOfTypeExtractors[pointerTypeType]
                        = baseTypeOfTypeExtractors[arrayTypeType]
                          = (Func<Type, Type>)(type => type == typeof(object) ? type.BaseType : (type.BaseType ?? typeof(object)));
            interfacesOfTypeExtractors[runtimeTypeType] = (Func<Type, Type[]>)(type => type.GetInterfaces());
            interfacesOfTypeExtractors[typeof(TypeBuilder)]
                = (Func<Type, Type[]>)(type => GetInterfaces(GetBaseType(type)).Concat(type.GetInterfaces()).Distinct().ToArray());
            interfacesOfTypeExtractors[typeof(GenericTypeParameterBuilder)] = (Func<Type, Type[]>)(type => type.GetGenericParameterConstraints());
            typeComparers[runtimeTypeType]
                = typeComparers[typeof(TypeBuilder)]
                  = typeComparers[typeof(GenericTypeParameterBuilder)] = (Func<Type, Type, bool>)((x, y) => x == y);
            typeComparers[byRefTypeType]
                = typeComparers[pointerTypeType]
                  = typeComparers[arrayTypeType]
                    = (Func<Type, Type, bool>)((x, y) =>
                        {
                            if(x.IsByRef && y.IsByRef)
                                return Equal(x.GetElementType(), y.GetElementType());
                            if(x.IsPointer && y.IsPointer)
                                return Equal(x.GetElementType(), y.GetElementType());
                            if(x.IsArray && y.IsArray)
                                return x.GetArrayRank() == y.GetArrayRank() && Equal(x.GetElementType(), y.GetElementType());
                            return x == y;
                        });
            hashCodeCalculators[runtimeTypeType]
                = hashCodeCalculators[typeof(TypeBuilder)]
                  = hashCodeCalculators[typeof(GenericTypeParameterBuilder)]
                    = (Func<Type, int>)(type => type.GetHashCode());
            assignabilityCheckers[runtimeTypeType] = (Func<Type, Type, bool>)((to, from) =>
                {
                    if(to.IsAssignableFrom(from)) return true;
                    if(Equal(to, from)) return true;
                    if(to.IsInterface)
                    {
                        var interfaces = GetInterfaces(from);
                        return interfaces.Any(interfaCe => Equal(interfaCe, to));
                    }
                    while(from != null)
                    {
                        if(Equal(to, from))
                            return true;
                        from = GetBaseType(from);
                    }
                    return false;
                });
            assignabilityCheckers[typeof(TypeBuilder)] = (Func<Type, Type, bool>)((to, from) => to.IsAssignableFrom(from));

            parameterTypesExtractors[typeof(MethodBuilder)]
                = (Func<MethodBase, Type[]>)(method => new MethodBuilderWrapper(method).ParameterTypes);
            parameterTypesExtractors[typeof(ConstructorBuilder)]
                = (Func<MethodBase, Type[]>)(method => new ConstructorBuilderWrapper(method).ParameterTypes);
            parameterTypesExtractors[methodOnTypeBuilderInstType]
                = (Func<MethodBase, Type[]>)(method => new MethodOnTypeBuilderInstWrapper(method).ParameterTypes);
            if(!isMono)
            {
                parameterTypesExtractors[methodBuilderInstType]
                    = (Func<MethodBase, Type[]>)(method => new MethodBuilderInstWrapper(method).ParameterTypes);
            }
            //parameterTypesExtractors[constructorOnTypeBuilderInstType] = (Func<MethodBase, Type[]>)(method => new ConstructorOnTypeBuilderInstWrapper(method).ParameterTypes);

            returnTypeExtractors[methodOnTypeBuilderInstType]
                = (Func<MethodInfo, Type>)(method => new MethodOnTypeBuilderInstWrapper(method).ReturnType);
            if(!isMono)
            {
                returnTypeExtractors[methodBuilderInstType]
                    = (Func<MethodInfo, Type>)(method => new MethodBuilderInstWrapper(method).ReturnType);
            }

            baseTypeOfTypeExtractors[typeBuilderInstType]
                = (Func<Type, Type>)(type => new TypeBuilderInstWrapper(type).BaseType);

            interfacesOfTypeExtractors[typeBuilderInstType]
                = (Func<Type, Type[]>)(type => new TypeBuilderInstWrapper(type).GetInterfaces());
            interfacesOfTypeExtractors[byRefTypeType]
                = interfacesOfTypeExtractors[pointerTypeType]
                  = interfacesOfTypeExtractors[arrayTypeType]
                    = (Func<Type, Type[]>)(type =>
                        {
                            if(!type.IsArray)
                                return new Type[0];
                            if(type.GetArrayRank() > 1)
                                return typeof(int[,]).GetInterfaces();
                            var elementType = type.GetElementType();
                            return typeof(int[]).GetInterfaces()
                                                .Select(interfaCe => interfaCe.IsGenericType
                                                                         ? interfaCe.GetGenericTypeDefinition().MakeGenericType(elementType)
                                                                         : interfaCe).ToArray();
                        });

            typeComparers[typeBuilderInstType]
                = (Func<Type, Type, bool>)((x, y) => new TypeBuilderInstWrapper(x).Equals(new TypeBuilderInstWrapper(y)));

            hashCodeCalculators[typeBuilderInstType]
                = (Func<Type, int>)(type => new TypeBuilderInstWrapper(type).GetHashCode());

            assignabilityCheckers[typeof(GenericTypeParameterBuilder)] = (Func<Type, Type, bool>)((to, from) => to == from);
            assignabilityCheckers[typeBuilderInstType]
                = (Func<Type, Type, bool>)((to, from) => new TypeBuilderInstWrapper(to).IsAssignableFrom(new TypeBuilderInstWrapper(from)));
        }

        internal static bool IsMono { get { return isMono; } }

        public static Type[] GetParameterTypes(MethodBase method)
        {
            var type = method.GetType();
            var extractor = (Func<MethodBase, Type[]>)parameterTypesExtractors[type];
            if(extractor == null)
                throw new NotSupportedException("Unable to extract parameter types of '" + type + "'");
            return extractor(method);
        }

        public static Type GetReturnType(MethodInfo method)
        {
            var type = method.GetType();
            var extractor = (Func<MethodInfo, Type>)returnTypeExtractors[type];
            if(extractor == null)
                throw new NotSupportedException("Unable to extract return type of '" + type + "'");
            return extractor(method);
        }

        public static Type[] GetInterfaces(Type type)
        {
            var t = type.GetType();
            var extractor = (Func<Type, Type[]>)interfacesOfTypeExtractors[t];
            if(extractor == null)
                throw new NotSupportedException("Unable to extract interfaces of '" + t + "'");
            return extractor(type);
        }

        public static Type GetBaseType(Type type)
        {
            var t = type.GetType();
            var extractor = (Func<Type, Type>)baseTypeOfTypeExtractors[t];
            if(extractor == null)
                throw new NotSupportedException("Unable to extract base type of '" + t + "'");
            return extractor(type);
        }

        public static bool Equal(Type firstType, Type secondType)
        {
            if(firstType == null || secondType == null)
                return firstType == null && secondType == null;
            var type = firstType.GetType();
            if(secondType.GetType() != type)
                return false;
            var comparer = (Func<Type, Type, bool>)typeComparers[type];
            if(comparer == null)
                throw new NotSupportedException("Unable to compare instances of '" + type + "'");
            return comparer(firstType, secondType);
        }

        public static bool Equal(Type[] first, Type[] second)
        {
            if(first.Length != second.Length)
                return false;
            for(var i = 0; i < first.Length; ++i)
            {
                if(!Equal(first[i], second[i]))
                    return false;
            }
            return true;
        }

        public static int CalcHashCode(Type type)
        {
            var t = type.GetType();
            var calculator = (Func<Type, int>)hashCodeCalculators[t];
            if(calculator == null)
                throw new NotSupportedException("Unable to calc hash code of '" + t + "'");
            return calculator(type);
        }

        public static bool IsAssignableFrom(Type to, Type from)
        {
            var type = to.GetType();
            if(from == null || (from.GetType() != type && type != runtimeTypeType))
                return false;
            var checker = (Func<Type, Type, bool>)assignabilityCheckers[type];
            if(checker == null)
                throw new NotSupportedException("Unable to check asssignability of '" + type + "'");
            return checker(to, from);
        }

        public static Type SubstituteGenericParameters(Type type, Type[] genericArguments, Type[] instantiation)
        {
            return SubstituteGenericParameters(new[] {type}, genericArguments, instantiation)[0];
        }

        public static Type[] SubstituteGenericParameters(Type[] types, Type[] genericArguments, Type[] instantiation)
        {
            if(genericArguments == null) return types;
            var dict = new Dictionary<Type, Type>();
            for(var i = 0; i < genericArguments.Length; i++)
            {
                var type = genericArguments[i];
                if(type.IsGenericParameter)
                {
                    Type current;
                    if(!dict.TryGetValue(type, out current))
                        dict.Add(type, instantiation[i]);
                    else if(current != instantiation[i])
                        throw new InvalidOperationException(string.Format("The same generic argument '{0}' is instantiated with two different types: '{1}' and '{2}'", type, current, instantiation[i]));
                }
            }
            var result = new Type[types.Length];
            for(var i = 0; i < types.Length; ++i)
                result[i] = SubstituteGenericParameters(types[i], dict);
            return result;
        }

        private static Type FindType(IEnumerable<Type> types, string name)
        {
            var type = types.FirstOrDefault(t => t.Name == name);
            if(type == null)
                throw new InvalidOperationException(string.Format("Type '{0}' is not found", name));
            return type;
        }

        private static Func<ConstructorBuilder, Type[]> BuildConstructorBuilderParameterTypesExtractor()
        {
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(Type[]), new[] {typeof(ConstructorBuilder)}, typeof(Formatter).Module, true);
            using(var il = new GroboIL(dynamicMethod))
            {
                var getParameterTypesMethod = typeof(ConstructorBuilder).GetMethod("GetParameterTypes", BindingFlags.Instance | BindingFlags.NonPublic);
                if(getParameterTypesMethod == null)
                    throw new MissingMethodException(typeof(ConstructorBuilder).Name, "GetParameterTypes");
                il.Ldarg(0);
                il.Call(getParameterTypesMethod);
                il.Ret();
            }
            return (Func<ConstructorBuilder, Type[]>)dynamicMethod.CreateDelegate(typeof(Func<ConstructorBuilder, Type[]>));
        }

        private static Type SubstituteGenericParameters(Type type, Dictionary<Type, Type> instantiation)
        {
            if(type == null) return null;
            if(type.IsGenericParameter)
            {
                Type substitute;
                return instantiation.TryGetValue(type, out substitute) ? substitute : type;
            }
            if(type.IsGenericType)
                return type.GetGenericTypeDefinition().MakeGenericType(type.GetGenericArguments().Select(t => SubstituteGenericParameters(t, instantiation)).ToArray());
            if(type.IsArray)
            {
                var rank = type.GetArrayRank();
                return rank == 1 ? SubstituteGenericParameters(type.GetElementType(), instantiation).MakeArrayType()
                           : SubstituteGenericParameters(type.GetElementType(), instantiation).MakeArrayType(rank);
            }
            if(type.IsByRef)
                return SubstituteGenericParameters(type.GetElementType(), instantiation).MakeByRefType();
            if(type.IsPointer)
                return SubstituteGenericParameters(type.GetElementType(), instantiation).MakePointerType();
            return type;
        }

        private static readonly bool isMono;

        private static readonly Type runtimeMethodInfoType;
        private static readonly Type runtimeGenericMethodInfoType;
        private static readonly Type runtimeConstructorInfoType;
        private static readonly Type runtimeTypeType;
        private static readonly Type typeBuilderInstType;
        private static readonly Type methodOnTypeBuilderInstType;
        private static readonly Type constructorOnTypeBuilderInstType;
        private static readonly Type methodBuilderInstType;

        private static readonly Hashtable parameterTypesExtractors;
        private static readonly Hashtable returnTypeExtractors;

        private static readonly Hashtable interfacesOfTypeExtractors;
        private static readonly Hashtable baseTypeOfTypeExtractors;

        private static readonly Hashtable typeComparers;
        private static readonly Hashtable hashCodeCalculators;
        private static readonly Hashtable assignabilityCheckers;

        public class TypesComparer : IEqualityComparer<Type>
        {
            public bool Equals(Type x, Type y)
            {
                return Equal(x, y);
            }

            public int GetHashCode(Type type)
            {
                return CalcHashCode(type);
            }
        }

        private class MethodBuilderWrapper
        {
            static MethodBuilderWrapper()
            {
                string parameterTypesFieldName = isMono ? "parameters" : "m_parameterTypes";
                var parameterTypesField = typeof(MethodBuilder).GetField(parameterTypesFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if(parameterTypesField == null)
                    throw new InvalidOperationException(string.Format("Field 'MethodBuilder.{0}' is not found", parameterTypesFieldName));
                m_parameterTypesExtractor = FieldsExtractor.GetExtractor<MethodBase, Type[]>(parameterTypesField);
            }

            public MethodBuilderWrapper(MethodBase inst)
            {
                this.inst = inst;
            }

            public Type[] ParameterTypes { get { return m_parameterTypesExtractor(inst); } }

            private readonly MethodBase inst;
            private static readonly Func<MethodBase, Type[]> m_parameterTypesExtractor;
        }

        private class ConstructorBuilderWrapper
        {
            static ConstructorBuilderWrapper()
            {
                var methodBuilderField = typeof(ConstructorBuilder).GetField("m_methodBuilder", BindingFlags.Instance | BindingFlags.NonPublic);
                if(methodBuilderField == null)
                    throw new InvalidOperationException("Field 'ConstructorBuilder.m_methodBuilder' is not found");
                m_methodBuilderExtractor = FieldsExtractor.GetExtractor<MethodBase, MethodInfo>(methodBuilderField);
            }

            public ConstructorBuilderWrapper(MethodBase inst)
            {
                this.inst = inst;
            }

            public Type[] ParameterTypes { get { return GetParameterTypes(m_methodBuilderExtractor(inst)); } }

            private readonly MethodBase inst;
            private static readonly Func<MethodBase, MethodInfo> m_methodBuilderExtractor;
        }

        private class TypeBuilderInstWrapper
        {
            static TypeBuilderInstWrapper()
            {
                string typeFieldName = isMono ? "generic_type" : "m_type";
                string instFieldName = isMono ? "type_arguments" : "m_inst";
                var typeField = typeBuilderInstType.GetField(typeFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if(typeField == null)
                    throw new InvalidOperationException(string.Format("Field '{0}.{1}' is not found", typeBuilderInstType.Name, typeFieldName));
                m_typeExtractor = FieldsExtractor.GetExtractor<Type, Type>(typeField);
                var instField = typeBuilderInstType.GetField(instFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if(instField == null)
                    throw new InvalidOperationException(string.Format("Field '{0}.{1}' is not found", typeBuilderInstType.Name, instFieldName));
                m_instExtractor = FieldsExtractor.GetExtractor<Type, Type[]>(instField);
            }

            public TypeBuilderInstWrapper(Type inst)
            {
                this.inst = inst;
            }

            public bool IsOk { get { return isATypeBuilderInst(inst); } }

            public override bool Equals(object obj)
            {
                var other = obj as TypeBuilderInstWrapper;
                if(other == null)
                    return false;
                return Equal(m_type, other.m_type) && Equal(m_inst, other.m_inst);
            }

            public override int GetHashCode()
            {
                var result = CalcHashCode(m_type);
                foreach(var arg in m_inst)
                {
                    unchecked
                    {
                        result = result * 397 + CalcHashCode(arg);
                    }
                }
                return result;
            }

            public Type[] GetInterfaces()
            {
                return SubstituteGenericParameters(ReflectionExtensions.GetInterfaces(m_type), m_type.GetGenericArguments(), m_inst);
            }

            public bool IsAssignableFrom(TypeBuilderInstWrapper from)
            {
                if(Equals(from)) return true;
                if(m_type.IsInterface)
                {
                    var interfaces = from.GetInterfaces();
                    return interfaces.Any(interfaCe => isATypeBuilderInst(interfaCe) && Equals(new TypeBuilderInstWrapper(interfaCe)));
                }
                while(from != null)
                {
                    if(Equals(from))
                        return true;
                    var baseType = from.BaseType;
                    if(isATypeBuilderInst(baseType))
                        from = new TypeBuilderInstWrapper(baseType);
                    else break;
                }
                return false;
            }

            public Type BaseType { get { return SubstituteGenericParameters(GetBaseType(m_type), m_type.GetGenericArguments(), m_inst); } }

            public Type m_type { get { return m_typeExtractor(inst); } }
            public Type[] m_inst { get { return m_instExtractor(inst); } }

            private static Func<Type, bool> BuildIsATypeBuilderInstChecker()
            {
                var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(bool), new[] {typeof(Type)}, typeof(ReflectionExtensions), true);
                using(var il = new GroboIL(dynamicMethod))
                {
                    il.Ldarg(0);
                    il.Isinst(typeBuilderInstType);
                    il.Ldnull();
                    il.Cgt(true);
                    il.Ret();
                }
                return (Func<Type, bool>)dynamicMethod.CreateDelegate(typeof(Func<Type, bool>));
            }

            private readonly Type inst;
            private static readonly Func<Type, Type> m_typeExtractor;
            private static readonly Func<Type, Type[]> m_instExtractor;

            private static readonly Func<Type, bool> isATypeBuilderInst = BuildIsATypeBuilderInstChecker();
        }

        private class MethodOnTypeBuilderInstWrapper
        {
            static MethodOnTypeBuilderInstWrapper()
            {
                string methodFieldName = isMono ? "base_method" : "m_method";
                string typeFieldName = isMono ? "instantiation" : "m_type";
                var methodField = methodOnTypeBuilderInstType.GetField(methodFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if(methodField == null)
                    throw new InvalidOperationException(string.Format("Field '{0}.{1}' is not found", methodOnTypeBuilderInstType.Name, methodFieldName));
                m_methodExtractor = FieldsExtractor.GetExtractor<MethodBase, MethodInfo>(methodField);
                var typeField = methodOnTypeBuilderInstType.GetField(typeFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if(typeField == null)
                    throw new InvalidOperationException(string.Format("Field '{0}.{1}' is not found", methodOnTypeBuilderInstType.Name, typeFieldName));
                m_typeExtractor = FieldsExtractor.GetExtractor<MethodBase, Type>(typeField);
                if(isMono)
                {
                    var genericMethodField = methodOnTypeBuilderInstType.GetField("generic_method_definition", BindingFlags.Instance | BindingFlags.NonPublic);
                    if(genericMethodField == null)
                        throw new InvalidOperationException(string.Format("Field '{0}.generic_method_definition' is not found", methodOnTypeBuilderInstType.Name));
                    m_genericMethodExtractor = FieldsExtractor.GetExtractor<MethodBase, MethodInfo>(genericMethodField);
                    var methodArgumentsField = methodOnTypeBuilderInstType.GetField("method_arguments", BindingFlags.Instance | BindingFlags.NonPublic);
                    if(methodArgumentsField == null)
                        throw new InvalidOperationException(string.Format("Field '{0}.method_arguments' is not found", methodOnTypeBuilderInstType.Name));
                    m_methodArgumentsExtractor = FieldsExtractor.GetExtractor<MethodBase, Type[]>(methodArgumentsField);
                }
            }

            public MethodOnTypeBuilderInstWrapper(MethodBase inst)
            {
                this.inst = inst;
            }

            public Type[] ParameterTypes
            {
                get
                {
                    var method = m_method;
                    var type = m_type;
                    var result = GetParameterTypes(method);
                    var typeInst = new TypeBuilderInstWrapper(type);
                    if(typeInst.IsOk)
                        result = SubstituteGenericParameters(result, typeInst.m_type.GetGenericArguments(), typeInst.m_inst);
                    if(isMono)
                    {
                        var methodArguments = m_methodArguments;
                        if(methodArguments != null)
                            result = SubstituteGenericParameters(result, (m_genericMethod ?? m_method).GetGenericArguments(), methodArguments);
                    }
                    return result;
                }
            }

            public Type ReturnType
            {
                get
                {
                    var method = m_method;
                    var type = m_type;
                    var result = GetReturnType(method);
                    var typeInst = new TypeBuilderInstWrapper(type);
                    if(typeInst.IsOk)
                        result = SubstituteGenericParameters(result, typeInst.m_type.GetGenericArguments(), typeInst.m_inst);
                    if(isMono)
                    {
                        var methodArguments = m_methodArguments;
                        if(methodArguments != null)
                            result = SubstituteGenericParameters(result, (m_genericMethod ?? m_method).GetGenericArguments(), methodArguments);
                    }
                    return result;
                }
            }

            public MethodInfo m_method { get { return m_methodExtractor(inst); } }
            public Type m_type { get { return m_typeExtractor(inst); } }
            public MethodInfo m_genericMethod { get { return m_genericMethodExtractor(inst); } }
            public Type[] m_methodArguments { get { return m_methodArgumentsExtractor(inst); } }
            private readonly MethodBase inst;

            private static readonly Func<MethodBase, MethodInfo> m_methodExtractor;
            private static readonly Func<MethodBase, Type> m_typeExtractor;
            private static readonly Func<MethodBase, MethodInfo> m_genericMethodExtractor;
            private static readonly Func<MethodBase, Type[]> m_methodArgumentsExtractor;
        }

        private class ConstructorOnTypeBuilderInstWrapper
        {
            static ConstructorOnTypeBuilderInstWrapper()
            {
                var ctorField = constructorOnTypeBuilderInstType.GetField("m_ctor", BindingFlags.Instance | BindingFlags.NonPublic);
                if(ctorField == null)
                    throw new InvalidOperationException(string.Format("Field '{0}.m_ctor' is not found", constructorOnTypeBuilderInstType.Name));
                m_ctorExtractor = FieldsExtractor.GetExtractor<MethodBase, MethodBase>(ctorField);
                var typeField = constructorOnTypeBuilderInstType.GetField("m_type", BindingFlags.Instance | BindingFlags.NonPublic);
                if(typeField == null)
                    throw new InvalidOperationException(string.Format("Field '{0}.m_type' is not found", constructorOnTypeBuilderInstType.Name));
                m_typeExtractor = FieldsExtractor.GetExtractor<MethodBase, Type>(typeField);
            }

            public ConstructorOnTypeBuilderInstWrapper(MethodBase inst)
            {
                this.inst = inst;
            }

            public Type[] ParameterTypes
            {
                get
                {
                    var typeInst = new TypeBuilderInstWrapper(m_type);
                    return SubstituteGenericParameters(GetParameterTypes(m_ctor), typeInst.m_type.GetGenericArguments(), typeInst.m_inst);
                }
            }

            public MethodBase m_ctor { get { return m_ctorExtractor(inst); } }
            public Type m_type { get { return m_typeExtractor(inst); } }
            private readonly MethodBase inst;
            private static readonly Func<MethodBase, MethodBase> m_ctorExtractor;
            private static readonly Func<MethodBase, Type> m_typeExtractor;
        }

        private class MethodBuilderInstWrapper
        {
            static MethodBuilderInstWrapper()
            {
                var methodField = methodBuilderInstType.GetField("m_method", BindingFlags.Instance | BindingFlags.NonPublic);
                if(methodField == null)
                    throw new InvalidOperationException(string.Format("Field '{0}.m_method' is not found", methodBuilderInstType.Name));
                m_methodExtractor = FieldsExtractor.GetExtractor<MethodBase, MethodInfo>(methodField);
                var instField = methodBuilderInstType.GetField("m_inst", BindingFlags.Instance | BindingFlags.NonPublic);
                if(instField == null)
                    throw new InvalidOperationException(string.Format("Field '{0}.m_inst' is not found", methodBuilderInstType.Name));
                m_instExtractor = FieldsExtractor.GetExtractor<MethodBase, Type[]>(instField);
            }

            public MethodBuilderInstWrapper(MethodBase inst)
            {
                this.inst = inst;
            }

            public Type[] ParameterTypes { get { return SubstituteGenericParameters(GetParameterTypes(m_method), m_method.GetGenericArguments(), m_inst); } }

            public Type ReturnType { get { return SubstituteGenericParameters(GetReturnType(m_method), m_method.GetGenericArguments(), m_inst); } }

            public MethodInfo m_method { get { return m_methodExtractor(inst); } }
            public Type[] m_inst { get { return m_instExtractor(inst); } }
            private readonly MethodBase inst;
            private static readonly Func<MethodBase, MethodInfo> m_methodExtractor;
            private static readonly Func<MethodBase, Type[]> m_instExtractor;
        }
    }
}