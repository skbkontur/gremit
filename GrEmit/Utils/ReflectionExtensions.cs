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
            IsMono = Type.GetType("Mono.Runtime") != null;

            var assembly = typeof(MethodInfo).Assembly;
            var types = assembly.GetTypes();

            var runtimeTypeType = FindType(types, "RuntimeType");
            var monoTypeType = !IsMono ? runtimeTypeType : FindType(types, "MonoType");
            runtimeTypeTypes = new HashSet<Type> {runtimeTypeType, monoTypeType};
            var byRefTypeType = FindType(types, IsMono ? "ByRefType" : "SymbolType");
            var pointerTypeType = FindType(types, IsMono ? "PointerType" : "SymbolType");
            var arrayTypeType = FindType(types, IsMono ? "ArrayType" : "SymbolType");

            typeBuilderInstType = FindType(types, "TypeBuilderInstantiation");

            var runtimeMethodInfoType = FindType(types, IsMono ? "MonoMethod" : "RuntimeMethodInfo");
            var runtimeGenericMethodInfoType = FindType(types,"RuntimeMethodInfo");
            var runtimeConstructorInfoType = FindType(types, IsMono ? "MonoCMethod" : "RuntimeConstructorInfo");
            var runtimeGenericConstructorInfoType = FindType(types, "RuntimeConstructorInfo");
            methodOnTypeBuilderInstType = FindType(types, IsMono ? "MethodOnTypeBuilderInst" : "MethodOnTypeBuilderInstantiation");
            constructorOnTypeBuilderInstType = FindType(types, IsMono ? "ConstructorOnTypeBuilderInst" : "ConstructorOnTypeBuilderInstantiation");
            if(!IsMono)
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
                    = parameterTypesExtractors[runtimeConstructorInfoType]
                      = parameterTypesExtractors[runtimeGenericConstructorInfoType]
                        = (Func<MethodBase, Type[]>)(method => method.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
            returnTypeExtractors[runtimeMethodInfoType]
                = returnTypeExtractors[runtimeGenericMethodInfoType]
                  = returnTypeExtractors[typeof(DynamicMethod)]
                    = returnTypeExtractors[typeof(MethodBuilder)]
                      = (Func<MethodInfo, Type>)(method => method.ReturnType);
            baseTypeOfTypeExtractors[runtimeTypeType]
                = baseTypeOfTypeExtractors[monoTypeType]
                  = baseTypeOfTypeExtractors[typeof(TypeBuilder)]
                    = baseTypeOfTypeExtractors[typeof(GenericTypeParameterBuilder)]
                      = baseTypeOfTypeExtractors[byRefTypeType]
                        = baseTypeOfTypeExtractors[pointerTypeType]
                          = baseTypeOfTypeExtractors[arrayTypeType]
                            = (Func<Type, Type>)(type => type == typeof(object) ? type.BaseType : (type.BaseType ?? typeof(object)));
            interfacesOfTypeExtractors[runtimeTypeType]
                = interfacesOfTypeExtractors[monoTypeType]
                  = (Func<Type, Type[]>)(type => type.GetInterfaces());
            interfacesOfTypeExtractors[typeof(TypeBuilder)]
                = (Func<Type, Type[]>)(type => GetInterfaces(GetBaseType(type)).Concat(type.GetInterfaces()).Distinct().ToArray());
            interfacesOfTypeExtractors[typeof(GenericTypeParameterBuilder)] = (Func<Type, Type[]>)(type => type.GetGenericParameterConstraints());
            typeComparers[runtimeTypeType]
                = typeComparers[monoTypeType]
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
                = hashCodeCalculators[monoTypeType]
                  = hashCodeCalculators[typeof(TypeBuilder)]
                    = hashCodeCalculators[typeof(GenericTypeParameterBuilder)]
                      = (Func<Type, int>)(type => type.GetHashCode());
            hashCodeCalculators[byRefTypeType]
              = hashCodeCalculators[pointerTypeType]
                = hashCodeCalculators[arrayTypeType]
                  = (Func<Type, int>)(type =>
                      {
                          if (type.IsByRef)
                              return CalcHashCode(type.GetElementType()) * 31 + 1;
                          if (type.IsPointer)
                              return CalcHashCode(type.GetElementType()) * 31 + 2;
                          if (type.IsArray)
                              return (CalcHashCode(type.GetElementType()) * 31 + type.GetArrayRank()) * 31 + 3;
                          return type.GetHashCode();
                      });
            assignabilityCheckers[runtimeTypeType]
                = assignabilityCheckers[monoTypeType]
                  = (Func<Type, Type, bool>)((to, from) =>
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
            if(!IsMono)
            {
                parameterTypesExtractors[methodBuilderInstType]
                    = (Func<MethodBase, Type[]>)(method => new MethodBuilderInstWrapper(method).ParameterTypes);
            }
            parameterTypesExtractors[constructorOnTypeBuilderInstType]
                = (Func<MethodBase, Type[]>)(method => new ConstructorOnTypeBuilderInstWrapper(method).ParameterTypes);

            returnTypeExtractors[methodOnTypeBuilderInstType]
                = (Func<MethodInfo, Type>)(method => new MethodOnTypeBuilderInstWrapper(method).ReturnType);
            if(!IsMono)
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
            assignabilityCheckers[byRefTypeType]
                = assignabilityCheckers[pointerTypeType]
                  = assignabilityCheckers[arrayTypeType]
                    = (Func<Type, Type, bool>)((to, from) =>
                        {
                            if(to.IsByRef && from.IsByRef)
                                return Equal(to.GetElementType(), from.GetElementType());
                            if(to.IsPointer && from.IsPointer)
                                return Equal(to.GetElementType(), from.GetElementType());
                            if(to.IsArray && from.IsArray)
                                return to.GetArrayRank() == from.GetArrayRank() && Equal(to.GetElementType(), from.GetElementType());
                            return to == from;
                        });
        }

        internal static bool IsMono { get; }

        public static Type[] GetParameterTypes(MethodBase method)
        {
            var type = method.GetType();
            var extractor = (Func<MethodBase, Type[]>)parameterTypesExtractors[type];
            if(extractor == null)
                throw new NotSupportedException($"Unable to extract parameter types of '{type}'");
            return extractor(method);
        }

        public static Type GetReturnType(MethodInfo method)
        {
            var type = method.GetType();
            var extractor = (Func<MethodInfo, Type>)returnTypeExtractors[type];
            if(extractor == null)
                throw new NotSupportedException($"Unable to extract return type of '{type}'");
            return extractor(method);
        }

        public static Type[] GetInterfaces(Type type)
        {
            var t = type.GetType();
            var extractor = (Func<Type, Type[]>)interfacesOfTypeExtractors[t];
            if(extractor == null)
                throw new NotSupportedException($"Unable to extract interfaces of '{t}'");
            return extractor(type);
        }

        public static Type GetBaseType(Type type)
        {
            var t = type.GetType();
            var extractor = (Func<Type, Type>)baseTypeOfTypeExtractors[t];
            if(extractor == null)
                throw new NotSupportedException($"Unable to extract base type of '{t}'");
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
                throw new NotSupportedException($"Unable to compare instances of '{type}'");
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
                throw new NotSupportedException($"Unable to calc hash code of '{t}'");
            return calculator(type);
        }

        public static bool IsAssignableFrom(Type to, Type from)
        {
            var type = to.GetType();
            if(from == null || (from.GetType() != type && !runtimeTypeTypes.Contains(type)))
                return false;
            var checker = (Func<Type, Type, bool>)assignabilityCheckers[type];
            if(checker == null)
                throw new NotSupportedException($"Unable to check asssignability of '{type}'");
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
                        throw new InvalidOperationException($"The same generic argument '{type}' is instantiated with two different types: '{current}' and '{instantiation[i]}'");
                }
            }
            var result = new Type[types.Length];
            for(var i = 0; i < types.Length; ++i)
                result[i] = SubstituteGenericParameters(types[i], dict);
            return result;
        }

        private static Type TryFindType(IEnumerable<Type> types, string name)
        {
            return types.FirstOrDefault(t => t.Name == name);
        }

        private static Type FindType(IEnumerable<Type> types, string name)
        {
            var type = TryFindType(types, name);
            if(type == null)
                throw new InvalidOperationException($"Type '{name}' is not found");
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

        private static readonly HashSet<Type> runtimeTypeTypes;
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
                string parameterTypesFieldName = IsMono ? "parameters" : "m_parameterTypes";
                var parameterTypesField = typeof(MethodBuilder).GetField(parameterTypesFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if(parameterTypesField == null)
                    throw new InvalidOperationException($"Field 'MethodBuilder.{parameterTypesFieldName}' is not found");
                m_parameterTypesExtractor = FieldsExtractor.GetExtractor<MethodBase, Type[]>(parameterTypesField);
            }

            public MethodBuilderWrapper(MethodBase inst)
            {
                this.inst = inst;
            }

            public Type[] ParameterTypes => m_parameterTypesExtractor(inst);

            private readonly MethodBase inst;
            private static readonly Func<MethodBase, Type[]> m_parameterTypesExtractor;
        }

        private class ConstructorBuilderWrapper
        {
            static ConstructorBuilderWrapper()
            {
                if(!IsMono)
                {
                    var methodBuilderField = typeof(ConstructorBuilder).GetField("m_methodBuilder", BindingFlags.Instance | BindingFlags.NonPublic);
                    if(methodBuilderField == null)
                        throw new InvalidOperationException("Field 'ConstructorBuilder.m_methodBuilder' is not found");
                    m_methodBuilderExtractor = FieldsExtractor.GetExtractor<MethodBase, MethodInfo>(methodBuilderField);
                }
                else
                {
                    string parameterTypesFieldName = "parameters";
                    var parameterTypesField = typeof(ConstructorBuilder).GetField(parameterTypesFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                    if(parameterTypesField == null)
                        throw new InvalidOperationException($"Field 'ConstructorBuilder.{parameterTypesFieldName}' is not found");
                    m_parameterTypesExtractor = FieldsExtractor.GetExtractor<MethodBase, Type[]>(parameterTypesField);
                }
            }

            public ConstructorBuilderWrapper(MethodBase inst)
            {
                this.inst = inst;
            }

            public Type[] ParameterTypes
            {
                get
                {
                    if(!IsMono)
                        return GetParameterTypes(m_methodBuilderExtractor(inst));
                    return m_parameterTypesExtractor(inst);
                }
            }

            private readonly MethodBase inst;
            private static readonly Func<MethodBase, MethodInfo> m_methodBuilderExtractor;
            private static readonly Func<MethodBase, Type[]> m_parameterTypesExtractor;
        }

        private class TypeBuilderInstWrapper
        {
            static TypeBuilderInstWrapper()
            {
                string typeFieldName = IsMono ? "generic_type" : "m_type";
                string instFieldName = IsMono ? "type_arguments" : "m_inst";
                var typeField = typeBuilderInstType.GetField(typeFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if(typeField == null)
                    throw new InvalidOperationException($"Field '{typeBuilderInstType.Name}.{typeFieldName}' is not found");
                m_typeExtractor = FieldsExtractor.GetExtractor<Type, Type>(typeField);
                var instField = typeBuilderInstType.GetField(instFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if(instField == null)
                    throw new InvalidOperationException($"Field '{typeBuilderInstType.Name}.{instFieldName}' is not found");
                m_instExtractor = FieldsExtractor.GetExtractor<Type, Type[]>(instField);
            }

            public TypeBuilderInstWrapper(Type inst)
            {
                this.inst = inst;
            }

            public bool IsOk => isATypeBuilderInst(inst);

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

            public Type BaseType => SubstituteGenericParameters(GetBaseType(m_type), m_type.GetGenericArguments(), m_inst);

            public Type m_type => m_typeExtractor(inst);
            public Type[] m_inst => m_instExtractor(inst);

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
                string methodFieldName = IsMono ? "base_method" : "m_method";
                string typeFieldName = IsMono ? "instantiation" : "m_type";
                var methodField = methodOnTypeBuilderInstType.GetField(methodFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if(methodField == null)
                    throw new InvalidOperationException($"Field '{methodOnTypeBuilderInstType.Name}.{methodFieldName}' is not found");
                m_methodExtractor = FieldsExtractor.GetExtractor<MethodBase, MethodInfo>(methodField);
                var typeField = methodOnTypeBuilderInstType.GetField(typeFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if(typeField == null)
                    throw new InvalidOperationException($"Field '{methodOnTypeBuilderInstType.Name}.{typeFieldName}' is not found");
                m_typeExtractor = FieldsExtractor.GetExtractor<MethodBase, Type>(typeField);
                if(IsMono)
                {
                    var genericMethodField = methodOnTypeBuilderInstType.GetField("generic_method_definition", BindingFlags.Instance | BindingFlags.NonPublic);
                    if(genericMethodField == null)
                        throw new InvalidOperationException($"Field '{methodOnTypeBuilderInstType.Name}.generic_method_definition' is not found");
                    m_genericMethodExtractor = FieldsExtractor.GetExtractor<MethodBase, MethodInfo>(genericMethodField);
                    var methodArgumentsField = methodOnTypeBuilderInstType.GetField("method_arguments", BindingFlags.Instance | BindingFlags.NonPublic);
                    if(methodArgumentsField == null)
                        throw new InvalidOperationException($"Field '{methodOnTypeBuilderInstType.Name}.method_arguments' is not found");
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
                    if(IsMono)
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
                    if(IsMono)
                    {
                        var methodArguments = m_methodArguments;
                        if(methodArguments != null)
                            result = SubstituteGenericParameters(result, (m_genericMethod ?? m_method).GetGenericArguments(), methodArguments);
                    }
                    return result;
                }
            }

            public MethodInfo m_method => m_methodExtractor(inst);
            public Type m_type => m_typeExtractor(inst);
            public MethodInfo m_genericMethod => m_genericMethodExtractor(inst);
            public Type[] m_methodArguments => m_methodArgumentsExtractor(inst);
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
                string ctorFieldName = IsMono ? "cb" : "m_ctor";
                string typeFieldName = IsMono ? "instantiation" : "m_type";
                var ctorField = constructorOnTypeBuilderInstType.GetField(ctorFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if(ctorField == null)
                    throw new InvalidOperationException($"Field '{constructorOnTypeBuilderInstType.Name}.{ctorFieldName}' is not found");
                m_ctorExtractor = FieldsExtractor.GetExtractor<MethodBase, MethodBase>(ctorField);
                var typeField = constructorOnTypeBuilderInstType.GetField(typeFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if(typeField == null)
                    throw new InvalidOperationException($"Field '{constructorOnTypeBuilderInstType.Name}.{typeFieldName}' is not found");
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
                    var result = GetParameterTypes(m_ctor);
                    var typeInst = new TypeBuilderInstWrapper(m_type);
                    if(typeInst.IsOk)
                        result = SubstituteGenericParameters(result, typeInst.m_type.GetGenericArguments(), typeInst.m_inst);
                    return result;
                }
            }

            public MethodBase m_ctor => m_ctorExtractor(inst);
            public Type m_type => m_typeExtractor(inst);
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
                    throw new InvalidOperationException($"Field '{methodBuilderInstType.Name}.m_method' is not found");
                m_methodExtractor = FieldsExtractor.GetExtractor<MethodBase, MethodInfo>(methodField);
                var instField = methodBuilderInstType.GetField("m_inst", BindingFlags.Instance | BindingFlags.NonPublic);
                if(instField == null)
                    throw new InvalidOperationException($"Field '{methodBuilderInstType.Name}.m_inst' is not found");
                m_instExtractor = FieldsExtractor.GetExtractor<MethodBase, Type[]>(instField);
            }

            public MethodBuilderInstWrapper(MethodBase inst)
            {
                this.inst = inst;
            }

            public Type[] ParameterTypes => SubstituteGenericParameters(GetParameterTypes(m_method), m_method.GetGenericArguments(), m_inst);

            public Type ReturnType => SubstituteGenericParameters(GetReturnType(m_method), m_method.GetGenericArguments(), m_inst);

            public MethodInfo m_method => m_methodExtractor(inst);
            public Type[] m_inst => m_instExtractor(inst);
            private readonly MethodBase inst;
            private static readonly Func<MethodBase, MethodInfo> m_methodExtractor;
            private static readonly Func<MethodBase, Type[]> m_instExtractor;
        }
    }
}