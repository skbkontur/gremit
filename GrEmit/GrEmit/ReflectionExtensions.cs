using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace GrEmit
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
            typeBuilderInstantiationType = assembly.GetTypes().FirstOrDefault(type => type.Name == "TypeBuilderInstantiation");
            if(typeBuilderInstantiationType == null)
                throw new InvalidOperationException("Type 'TypeBuilderInstantiation' is not found");

            runtimeMethodInfoType = types.FirstOrDefault(type => type.Name == "RuntimeMethodInfo");
            if(runtimeMethodInfoType == null)
                throw new InvalidOperationException("Type 'RuntimeMethodInfo' is not found");
            methodOnTypeBuilderInstantiationType = types.FirstOrDefault(type => type.Name == "MethodOnTypeBuilderInstantiation");
            if(methodOnTypeBuilderInstantiationType == null)
                throw new InvalidOperationException("Type 'MethodOnTypeBuilderInstantiation' is not found");
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
            parameterTypesExtractors[runtimeMethodInfoType] = parameterTypesExtractors[typeof(DynamicMethod)] = (Func<MethodInfo, Type[]>)(method => method.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
            returnTypeExtractors[runtimeMethodInfoType] = returnTypeExtractors[typeof(DynamicMethod)] = returnTypeExtractors[typeof(MethodBuilder)] = (Func<MethodInfo, Type>)(method => method.ReturnType);
            baseTypeOfTypeExtractors[runtimeTypeType] = baseTypeOfTypeExtractors[typeof(TypeBuilder)] = (Func<Type, Type>)(type => type.BaseType);
            interfacesOfTypeExtractors[runtimeTypeType] = (Func<Type, Type[]>)(type => type.GetInterfaces());
            interfacesOfTypeExtractors[typeof(TypeBuilder)] = (Func<Type, Type[]>)(type => GetInterfaces(GetBaseType(type)).Concat(type.GetInterfaces()).Distinct().ToArray());
            typeComparers[runtimeTypeType] = typeComparers[typeof(TypeBuilder)] = typeComparers[typeof(GenericTypeParameterBuilder)] = (Func<Type, Type, bool>)((x, y) => x == y);
            hashCodeCalculators[runtimeTypeType] = hashCodeCalculators[typeof(TypeBuilder)] = hashCodeCalculators[typeof(GenericTypeParameterBuilder)] = (Func<Type, int>)(type => type.GetHashCode());
            assignabilityCheckers[runtimeTypeType] = assignabilityCheckers[typeof(TypeBuilder)] = (Func<Type, Type, bool>)((to, from) => to.IsAssignableFrom(from));

            parameterTypesExtractors[typeof(MethodBuilder)] = (Func<MethodInfo, Type[]>)(method => new MethodBuilderWrapper(method).ParameterTypes);
            parameterTypesExtractors[methodOnTypeBuilderInstantiationType] = (Func<MethodInfo, Type[]>)(method => new MethodOnTypeBuilderInstantiationWrapper(method).ParameterTypes);
            parameterTypesExtractors[methodBuilderInstantiationType] = (Func<MethodInfo, Type[]>)(method => new MethodBuilderInstantiationWrapper(method).ParameterTypes);

            returnTypeExtractors[methodOnTypeBuilderInstantiationType] = (Func<MethodInfo, Type>)(method => new MethodOnTypeBuilderInstantiationWrapper(method).ReturnType);
            returnTypeExtractors[methodBuilderInstantiationType] = (Func<MethodInfo, Type>)(method => new MethodBuilderInstantiationWrapper(method).ReturnType);

            constructorBuilderParameterTypesExtractor = BuildConstructorBuilderParameterTypesExtractor();

            baseTypeOfTypeExtractors[typeBuilderInstantiationType] = (Func<Type, Type>)(type => new TypeBuilderInstantiationWrapper(type).BaseType);

            interfacesOfTypeExtractors[typeBuilderInstantiationType] = (Func<Type, Type[]>)(type => new TypeBuilderInstantiationWrapper(type).GetInterfaces());

            typeComparers[typeBuilderInstantiationType] = (Func<Type, Type, bool>)((x, y) => new TypeBuilderInstantiationWrapper(x).Equals(new TypeBuilderInstantiationWrapper(y)));

            hashCodeCalculators[typeBuilderInstantiationType] = (Func<Type, int>)(type => new TypeBuilderInstantiationWrapper(type).GetHashCode());

            assignabilityCheckers[typeof(GenericTypeParameterBuilder)] = (Func<Type, Type, bool>)((to, from) => to == from);
            assignabilityCheckers[typeBuilderInstantiationType] = (Func<Type, Type, bool>)((to, from) => new TypeBuilderInstantiationWrapper(to).IsAssignableFrom(new TypeBuilderInstantiationWrapper(from)));
        }

        public static Type[] GetParameterTypes(MethodInfo method)
        {
            var type = method.GetType();
            var extractor = (Func<MethodInfo, Type[]>)parameterTypesExtractors[type];
            if(extractor == null)
                throw new NotSupportedException("Unable to extract parameter types of '" + type + "'");
            return extractor(method);
        }

        public static Type[] GetParameterTypes(ConstructorInfo constructor)
        {
            if(constructor is ConstructorBuilder)
                return constructorBuilderParameterTypesExtractor((ConstructorBuilder)constructor);
            return constructor.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
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
            if(from == null || from.GetType() != type)
                return false;
            var checker = (Func<Type, Type, bool>)assignabilityCheckers[type];
            if(checker == null)
                throw new NotSupportedException("Unable to check asssignability of '" + type + "'");
            return checker(to, from);
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

        private static Type SubstituteGenericParameters(Type type, Type[] genericArguments, Type[] instantiation)
        {
            return SubstituteGenericParameters(new[] {type}, genericArguments, instantiation)[0];
        }

        private static Type[] SubstituteGenericParameters(Type[] types, Type[] genericArguments, Type[] instantiation)
        {
            var dict = new Dictionary<Type, Type>();
            for(var i = 0; i < genericArguments.Length; i++)
            {
                var type = genericArguments[i];
                if(type.IsGenericParameter)
                    dict.Add(type, instantiation[i]);
            }
            var result = new Type[types.Length];
            for(var i = 0; i < types.Length; ++i)
                result[i] = SubstituteGenericParameters(types[i], dict);
            return result;
        }

        private static readonly Type runtimeMethodInfoType;
        private static readonly Type runtimeTypeType;
        private static readonly Type typeBuilderInstantiationType;
        private static readonly Type methodOnTypeBuilderInstantiationType;
        private static readonly Type methodBuilderInstantiationType;

        private static readonly Hashtable parameterTypesExtractors;
        private static readonly Func<ConstructorBuilder, Type[]> constructorBuilderParameterTypesExtractor;
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
                m_parameterTypesExtractor = FieldsExtractor.GetExtractor<MethodInfo, Type[]>(methodBuilderParameterTypesField);
            }

            public MethodBuilderWrapper(MethodInfo inst)
            {
                this.inst = inst;
            }

            public Type[] ParameterTypes { get { return m_parameterTypesExtractor(inst); } }

            private readonly MethodInfo inst;
            private static readonly Func<MethodInfo, Type[]> m_parameterTypesExtractor;
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
                if(!ReflectionExtensions.IsAssignableFrom(m_type, from.m_type))
                    return false;
                var ourInst = m_inst;
                var otherInst = from.m_inst;
                for(var i = 0; i < ourInst.Length; ++i)
                {
                    if(!ReflectionExtensions.IsAssignableFrom(ourInst[i], otherInst[i]))
                        return false;
                }
                return true;
            }

            public Type BaseType { get { return SubstituteGenericParameters(GetBaseType(m_type), m_type.GetGenericArguments(), m_inst); } }

            public Type m_type { get { return m_typeExtractor(inst); } }
            public Type[] m_inst { get { return m_instExtractor(inst); } }
            private readonly Type inst;
            private static readonly Func<Type, Type> m_typeExtractor;
            private static readonly Func<Type, Type[]> m_instExtractor;
        }

        private class MethodOnTypeBuilderInstantiationWrapper
        {
            static MethodOnTypeBuilderInstantiationWrapper()
            {
                var methodOnTypeBuilderInstantiationMethodField = methodOnTypeBuilderInstantiationType.GetField("m_method", BindingFlags.Instance | BindingFlags.NonPublic);
                if(methodOnTypeBuilderInstantiationMethodField == null)
                    throw new InvalidOperationException("Field 'MethodOnTypeBuilderInstantiation.m_method' is not found");
                m_methodExtractor = FieldsExtractor.GetExtractor<MethodInfo, MethodInfo>(methodOnTypeBuilderInstantiationMethodField);
                var methodOnTypeBuilderInstantiationTypeField = methodOnTypeBuilderInstantiationType.GetField("m_type", BindingFlags.Instance | BindingFlags.NonPublic);
                if(methodOnTypeBuilderInstantiationTypeField == null)
                    throw new InvalidOperationException("Field 'MethodOnTypeBuilderInstantiation.m_type' is not found");
                m_typeExtractor = FieldsExtractor.GetExtractor<MethodInfo, Type>(methodOnTypeBuilderInstantiationTypeField);
            }

            public MethodOnTypeBuilderInstantiationWrapper(MethodInfo inst)
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
            private readonly MethodInfo inst;
            private static readonly Func<MethodInfo, MethodInfo> m_methodExtractor;
            private static readonly Func<MethodInfo, Type> m_typeExtractor;
        }

        private class MethodBuilderInstantiationWrapper
        {
            static MethodBuilderInstantiationWrapper()
            {
                var methodBuilderInstantiationMethodField = methodBuilderInstantiationType.GetField("m_method", BindingFlags.Instance | BindingFlags.NonPublic);
                if(methodBuilderInstantiationMethodField == null)
                    throw new InvalidOperationException("Field 'MethodBuilderInstantiation.m_method' is not found");
                m_methodExtractor = FieldsExtractor.GetExtractor<MethodInfo, MethodInfo>(methodBuilderInstantiationMethodField);
                var methodBuilderInstantiationInstField = methodBuilderInstantiationType.GetField("m_inst", BindingFlags.Instance | BindingFlags.NonPublic);
                if(methodBuilderInstantiationInstField == null)
                    throw new InvalidOperationException("Field 'MethodBuilderInstantiation.m_inst' is not found");
                m_instExtractor = FieldsExtractor.GetExtractor<MethodInfo, Type[]>(methodBuilderInstantiationInstField);
            }

            public MethodBuilderInstantiationWrapper(MethodInfo inst)
            {
                this.inst = inst;
            }

            public Type[] ParameterTypes { get { return SubstituteGenericParameters(GetParameterTypes(m_method), m_method.GetGenericArguments(), m_inst); } }

            public Type ReturnType { get { return SubstituteGenericParameters(GetReturnType(m_method), m_method.GetGenericArguments(), m_inst); } }

            public MethodInfo m_method { get { return m_methodExtractor(inst); } }
            public Type[] m_inst { get { return m_instExtractor(inst); } }
            private readonly MethodInfo inst;
            private static readonly Func<MethodInfo, MethodInfo> m_methodExtractor;
            private static readonly Func<MethodInfo, Type[]> m_instExtractor;
        }
    }
}