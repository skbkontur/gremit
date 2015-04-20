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
            var assembly = typeof(MethodInfo).Assembly;
            var types = assembly.GetTypes();

            runtimeTypeType = types.FirstOrDefault(type => type.Name == "RuntimeType");
            if(runtimeTypeType == null)
                throw new InvalidOperationException("Type 'RuntimeType' is not found");
            symbolTypeType = types.FirstOrDefault(type => type.Name == "SymbolType");
            if(symbolTypeType == null)
                throw new InvalidOperationException("Type 'SymbolType' is not found");
            typeBuilderInstantiationType = assembly.GetTypes().FirstOrDefault(type => type.Name == "TypeBuilderInstantiation");
            if(typeBuilderInstantiationType == null)
                throw new InvalidOperationException("Type 'TypeBuilderInstantiation' is not found");

            runtimeMethodInfoType = types.FirstOrDefault(type => type.Name == "RuntimeMethodInfo");
            if(runtimeMethodInfoType == null)
                throw new InvalidOperationException("Type 'RuntimeMethodInfo' is not found");
            runtimeConstructorInfoType = types.FirstOrDefault(type => type.Name == "RuntimeConstructorInfo");
            if(runtimeConstructorInfoType == null)
                throw new InvalidOperationException("Type 'RuntimeConstructorInfo' is not found");
            methodOnTypeBuilderInstantiationType = types.FirstOrDefault(type => type.Name == "MethodOnTypeBuilderInstantiation");
            if(methodOnTypeBuilderInstantiationType == null)
                throw new InvalidOperationException("Type 'MethodOnTypeBuilderInstantiation' is not found");
            constructorOnTypeBuilderInstantiationType = types.FirstOrDefault(type => type.Name == "ConstructorOnTypeBuilderInstantiation");
            if(constructorOnTypeBuilderInstantiationType == null)
                throw new InvalidOperationException("Type 'ConstructorOnTypeBuilderInstantiation' is not found");
            methodBuilderInstantiationType = types.FirstOrDefault(type => type.Name == "MethodBuilderInstantiation");
            if(methodBuilderInstantiationType == null)
                throw new InvalidOperationException("Type 'MethodBuilderInstantiation' is not found");

            parameterTypesExtractors = new Hashtable();
            returnTypeExtractors = new Hashtable();
            baseTypeOfTypeExtractors = new Hashtable();
            interfacesOfTypeExtractors = new Hashtable();
            typeComparers = new Hashtable();
            hashCodeCalculators = new Hashtable();
            assignabilityCheckers = new Hashtable();
            parameterTypesExtractors[runtimeMethodInfoType] = parameterTypesExtractors[typeof(DynamicMethod)] = parameterTypesExtractors[runtimeConstructorInfoType] =
                                                                                                                (Func<MethodBase, Type[]>)(method => method.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
            returnTypeExtractors[runtimeMethodInfoType] = returnTypeExtractors[typeof(DynamicMethod)] = returnTypeExtractors[typeof(MethodBuilder)] =
                                                                                                        (Func<MethodInfo, Type>)(method => method.ReturnType);
            baseTypeOfTypeExtractors[runtimeTypeType] = baseTypeOfTypeExtractors[typeof(TypeBuilder)]
                                                        = baseTypeOfTypeExtractors[typeof(GenericTypeParameterBuilder)] = baseTypeOfTypeExtractors[symbolTypeType]
                                                                                                                          = (Func<Type, Type>)(type => type == typeof(object) ? type.BaseType : (type.BaseType ?? typeof(object)));
            interfacesOfTypeExtractors[runtimeTypeType] = (Func<Type, Type[]>)(type => type.GetInterfaces());
            interfacesOfTypeExtractors[typeof(TypeBuilder)] = (Func<Type, Type[]>)(type => GetInterfaces(GetBaseType(type)).Concat(type.GetInterfaces()).Distinct().ToArray());
            interfacesOfTypeExtractors[typeof(GenericTypeParameterBuilder)] = (Func<Type, Type[]>)(type => type.GetGenericParameterConstraints());
            typeComparers[runtimeTypeType] = typeComparers[typeof(TypeBuilder)] = typeComparers[typeof(GenericTypeParameterBuilder)] = (Func<Type, Type, bool>)((x, y) => x == y);
            typeComparers[symbolTypeType] = (Func<Type, Type, bool>)((x, y) =>
                {
                    if(x.IsByRef && y.IsByRef)
                        return Equal(x.GetElementType(), y.GetElementType());
                    if(x.IsPointer && y.IsPointer)
                        return Equal(x.GetElementType(), y.GetElementType());
                    if(x.IsArray && y.IsArray)
                        return x.GetArrayRank() == y.GetArrayRank() && Equal(x.GetElementType(), y.GetElementType());
                    return x == y;
                });
            hashCodeCalculators[runtimeTypeType] = hashCodeCalculators[typeof(TypeBuilder)] = hashCodeCalculators[typeof(GenericTypeParameterBuilder)] = (Func<Type, int>)(type => type.GetHashCode());
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

            parameterTypesExtractors[typeof(MethodBuilder)] = (Func<MethodBase, Type[]>)(method => new MethodBuilderWrapper(method).ParameterTypes);
            parameterTypesExtractors[typeof(ConstructorBuilder)] = (Func<MethodBase, Type[]>)(method => new ConstructorBuilderWrapper(method).ParameterTypes);
            parameterTypesExtractors[methodOnTypeBuilderInstantiationType] = (Func<MethodBase, Type[]>)(method => new MethodOnTypeBuilderInstantiationWrapper(method).ParameterTypes);
            parameterTypesExtractors[methodBuilderInstantiationType] = (Func<MethodBase, Type[]>)(method => new MethodBuilderInstantiationWrapper(method).ParameterTypes);
            parameterTypesExtractors[constructorOnTypeBuilderInstantiationType] = (Func<MethodBase, Type[]>)(method => new ConstructorOnTypeBuilderInstantiationWrapper(method).ParameterTypes);

            returnTypeExtractors[methodOnTypeBuilderInstantiationType] = (Func<MethodInfo, Type>)(method => new MethodOnTypeBuilderInstantiationWrapper(method).ReturnType);
            returnTypeExtractors[methodBuilderInstantiationType] = (Func<MethodInfo, Type>)(method => new MethodBuilderInstantiationWrapper(method).ReturnType);

            baseTypeOfTypeExtractors[typeBuilderInstantiationType] = (Func<Type, Type>)(type => new TypeBuilderInstantiationWrapper(type).BaseType);

            interfacesOfTypeExtractors[typeBuilderInstantiationType] = (Func<Type, Type[]>)(type => new TypeBuilderInstantiationWrapper(type).GetInterfaces());
            interfacesOfTypeExtractors[symbolTypeType] = (Func<Type, Type[]>)(type =>
                {
                    if(!type.IsArray)
                        return new Type[0];
                    if(type.GetArrayRank() > 1)
                        return typeof(int[,]).GetInterfaces();
                    var elementType = type.GetElementType();
                    return typeof(int[]).GetInterfaces().Select(interfaCe => interfaCe.IsGenericType ? interfaCe.GetGenericTypeDefinition().MakeGenericType(elementType) : interfaCe).ToArray();
                });

            typeComparers[typeBuilderInstantiationType] = (Func<Type, Type, bool>)((x, y) => new TypeBuilderInstantiationWrapper(x).Equals(new TypeBuilderInstantiationWrapper(y)));

            hashCodeCalculators[typeBuilderInstantiationType] = (Func<Type, int>)(type => new TypeBuilderInstantiationWrapper(type).GetHashCode());

            assignabilityCheckers[typeof(GenericTypeParameterBuilder)] = (Func<Type, Type, bool>)((to, from) => to == from);
            assignabilityCheckers[typeBuilderInstantiationType] = (Func<Type, Type, bool>)((to, from) => new TypeBuilderInstantiationWrapper(to).IsAssignableFrom(new TypeBuilderInstantiationWrapper(from)));
        }

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

        private static readonly Type runtimeMethodInfoType;
        private static readonly Type runtimeConstructorInfoType;
        private static readonly Type runtimeTypeType;
        private static readonly Type symbolTypeType;
        private static readonly Type typeBuilderInstantiationType;
        private static readonly Type methodOnTypeBuilderInstantiationType;
        private static readonly Type constructorOnTypeBuilderInstantiationType;
        private static readonly Type methodBuilderInstantiationType;

        private static readonly Hashtable parameterTypesExtractors;
        private static readonly Hashtable returnTypeExtractors;

        private static readonly Hashtable interfacesOfTypeExtractors;
        private static readonly Hashtable baseTypeOfTypeExtractors;

        private static readonly Hashtable typeComparers;
        private static readonly Hashtable hashCodeCalculators;
        private static readonly Hashtable assignabilityCheckers;

        private class MethodBuilderWrapper
        {
            static MethodBuilderWrapper()
            {
                var methodBuilderParameterTypesField = typeof(MethodBuilder).GetField("m_parameterTypes", BindingFlags.Instance | BindingFlags.NonPublic);
                if(methodBuilderParameterTypesField == null)
                    throw new InvalidOperationException("Field 'MethodBuilder.m_parameterTypes' is not found");
                m_parameterTypesExtractor = FieldsExtractor.GetExtractor<MethodBase, Type[]>(methodBuilderParameterTypesField);
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
                var constructorBuilderMethodBuilderField = typeof(ConstructorBuilder).GetField("m_methodBuilder", BindingFlags.Instance | BindingFlags.NonPublic);
                if(constructorBuilderMethodBuilderField == null)
                    throw new InvalidOperationException("Field 'ConstructorBuilder.m_methodBuilder' is not found");
                m_methodBuilderExtractor = FieldsExtractor.GetExtractor<MethodBase, MethodInfo>(constructorBuilderMethodBuilderField);
            }

            public ConstructorBuilderWrapper(MethodBase inst)
            {
                this.inst = inst;
            }

            public Type[] ParameterTypes { get { return GetParameterTypes(m_methodBuilderExtractor(inst)); } }

            private readonly MethodBase inst;
            private static readonly Func<MethodBase, MethodInfo> m_methodBuilderExtractor;
        }

        private class TypeBuilderInstantiationWrapper
        {
            static TypeBuilderInstantiationWrapper()
            {
                var typeBuilderInstantiationTypeField = typeBuilderInstantiationType.GetField("m_type", BindingFlags.Instance | BindingFlags.NonPublic);
                if(typeBuilderInstantiationTypeField == null)
                    throw new InvalidOperationException("Field 'TypeBuilderInstantiation.m_type' is not found");
                m_typeExtractor = FieldsExtractor.GetExtractor<Type, Type>(typeBuilderInstantiationTypeField);
                var typeBuilderInstantiationInstField = typeBuilderInstantiationType.GetField("m_inst", BindingFlags.Instance | BindingFlags.NonPublic);
                if(typeBuilderInstantiationInstField == null)
                    throw new InvalidOperationException("Field 'TypeBuilderInstantiation.m_inst' is not found");
                m_instExtractor = FieldsExtractor.GetExtractor<Type, Type[]>(typeBuilderInstantiationInstField);
            }

            public TypeBuilderInstantiationWrapper(Type inst)
            {
                this.inst = inst;
            }

            public override bool Equals(object obj)
            {
                var other = obj as TypeBuilderInstantiationWrapper;
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

            public bool IsAssignableFrom(TypeBuilderInstantiationWrapper from)
            {
                if(Equals(from)) return true;
                if(m_type.IsInterface)
                {
                    var interfaces = from.GetInterfaces();
                    return interfaces.Any(interfaCe => isATypeBuilderInstantiation(interfaCe) && Equals(new TypeBuilderInstantiationWrapper(interfaCe)));
                }
                while(from != null)
                {
                    if(Equals(from))
                        return true;
                    var baseType = from.BaseType;
                    if(isATypeBuilderInstantiation(baseType))
                        from = new TypeBuilderInstantiationWrapper(baseType);
                    else break;
                }
                return false;
            }

            public Type BaseType { get { return SubstituteGenericParameters(GetBaseType(m_type), m_type.GetGenericArguments(), m_inst); } }

            public Type m_type { get { return m_typeExtractor(inst); } }
            public Type[] m_inst { get { return m_instExtractor(inst); } }

            private static Func<Type, bool> BuildIsATypeBuilderInstantiationChecker()
            {
                var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(bool), new[] {typeof(Type)}, typeof(ReflectionExtensions), true);
                using(var il = new GroboIL(dynamicMethod))
                {
                    il.Ldarg(0);
                    il.Castclass(typeBuilderInstantiationType);
                    il.Ldnull();
                    il.Cgt(true);
                    il.Ret();
                }
                return (Func<Type, bool>)dynamicMethod.CreateDelegate(typeof(Func<Type, bool>));
            }

            private readonly Type inst;
            private static readonly Func<Type, Type> m_typeExtractor;
            private static readonly Func<Type, Type[]> m_instExtractor;
            private static readonly Func<Type, bool> isATypeBuilderInstantiation = BuildIsATypeBuilderInstantiationChecker();
        }

        private class MethodOnTypeBuilderInstantiationWrapper
        {
            static MethodOnTypeBuilderInstantiationWrapper()
            {
                var methodOnTypeBuilderInstantiationMethodField = methodOnTypeBuilderInstantiationType.GetField("m_method", BindingFlags.Instance | BindingFlags.NonPublic);
                if(methodOnTypeBuilderInstantiationMethodField == null)
                    throw new InvalidOperationException("Field 'MethodOnTypeBuilderInstantiation.m_method' is not found");
                m_methodExtractor = FieldsExtractor.GetExtractor<MethodBase, MethodInfo>(methodOnTypeBuilderInstantiationMethodField);
                var methodOnTypeBuilderInstantiationTypeField = methodOnTypeBuilderInstantiationType.GetField("m_type", BindingFlags.Instance | BindingFlags.NonPublic);
                if(methodOnTypeBuilderInstantiationTypeField == null)
                    throw new InvalidOperationException("Field 'MethodOnTypeBuilderInstantiation.m_type' is not found");
                m_typeExtractor = FieldsExtractor.GetExtractor<MethodBase, Type>(methodOnTypeBuilderInstantiationTypeField);
            }

            public MethodOnTypeBuilderInstantiationWrapper(MethodBase inst)
            {
                this.inst = inst;
            }

            public Type[] ParameterTypes
            {
                get
                {
                    var typeInst = new TypeBuilderInstantiationWrapper(m_type);
                    return SubstituteGenericParameters(GetParameterTypes(m_method), typeInst.m_type.GetGenericArguments(), typeInst.m_inst);
                }
            }

            public Type ReturnType
            {
                get
                {
                    var typeInst = new TypeBuilderInstantiationWrapper(m_type);
                    return SubstituteGenericParameters(GetReturnType(m_method), typeInst.m_type.GetGenericArguments(), typeInst.m_inst);
                }
            }

            public MethodInfo m_method { get { return m_methodExtractor(inst); } }
            public Type m_type { get { return m_typeExtractor(inst); } }
            private readonly MethodBase inst;
            private static readonly Func<MethodBase, MethodInfo> m_methodExtractor;
            private static readonly Func<MethodBase, Type> m_typeExtractor;
        }

        private class ConstructorOnTypeBuilderInstantiationWrapper
        {
            static ConstructorOnTypeBuilderInstantiationWrapper()
            {
                var constructorOnTypeBuilderInstantiationCtorField = constructorOnTypeBuilderInstantiationType.GetField("m_ctor", BindingFlags.Instance | BindingFlags.NonPublic);
                if(constructorOnTypeBuilderInstantiationCtorField == null)
                    throw new InvalidOperationException("Field 'ConstructorOnTypeBuilderInstantiation.m_ctor' is not found");
                m_ctorExtractor = FieldsExtractor.GetExtractor<MethodBase, MethodBase>(constructorOnTypeBuilderInstantiationCtorField);
                var constructorOnTypeBuilderInstantiationTypeField = constructorOnTypeBuilderInstantiationType.GetField("m_type", BindingFlags.Instance | BindingFlags.NonPublic);
                if(constructorOnTypeBuilderInstantiationTypeField == null)
                    throw new InvalidOperationException("Field 'ConstructorOnTypeBuilderInstantiation.m_type' is not found");
                m_typeExtractor = FieldsExtractor.GetExtractor<MethodBase, Type>(constructorOnTypeBuilderInstantiationTypeField);
            }

            public ConstructorOnTypeBuilderInstantiationWrapper(MethodBase inst)
            {
                this.inst = inst;
            }

            public Type[] ParameterTypes
            {
                get
                {
                    var typeInst = new TypeBuilderInstantiationWrapper(m_type);
                    return SubstituteGenericParameters(GetParameterTypes(m_ctor), typeInst.m_type.GetGenericArguments(), typeInst.m_inst);
                }
            }

            public MethodBase m_ctor { get { return m_ctorExtractor(inst); } }
            public Type m_type { get { return m_typeExtractor(inst); } }
            private readonly MethodBase inst;
            private static readonly Func<MethodBase, MethodBase> m_ctorExtractor;
            private static readonly Func<MethodBase, Type> m_typeExtractor;
        }

        private class MethodBuilderInstantiationWrapper
        {
            static MethodBuilderInstantiationWrapper()
            {
                var methodBuilderInstantiationMethodField = methodBuilderInstantiationType.GetField("m_method", BindingFlags.Instance | BindingFlags.NonPublic);
                if(methodBuilderInstantiationMethodField == null)
                    throw new InvalidOperationException("Field 'MethodBuilderInstantiation.m_method' is not found");
                m_methodExtractor = FieldsExtractor.GetExtractor<MethodBase, MethodInfo>(methodBuilderInstantiationMethodField);
                var methodBuilderInstantiationInstField = methodBuilderInstantiationType.GetField("m_inst", BindingFlags.Instance | BindingFlags.NonPublic);
                if(methodBuilderInstantiationInstField == null)
                    throw new InvalidOperationException("Field 'MethodBuilderInstantiation.m_inst' is not found");
                m_instExtractor = FieldsExtractor.GetExtractor<MethodBase, Type[]>(methodBuilderInstantiationInstField);
            }

            public MethodBuilderInstantiationWrapper(MethodBase inst)
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