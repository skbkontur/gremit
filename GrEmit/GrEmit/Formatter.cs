using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using GrEmit.Utils;

namespace GrEmit
{
    public static class Formatter
    {
        public static string Format(Type type)
        {
            if(type == null)
                return "null";
            if(type.IsByRef)
                return Format(type.GetElementType()) + "&";
            if(type.IsPointer)
                return Format(type.GetElementType()) + "*";
            if(!type.IsGenericType)
                return type.Name;
            var index = type.Name.LastIndexOf('`');
            return (index < 0 ? type.Name : type.Name.Substring(0, index)) + "<" + string.Join(", ", type.GetGenericArguments().Select(Format).ToArray()) + ">";
        }

        public static string Format(FieldInfo field)
        {
            return Format(field.ReflectedType) + "." + field.Name;
        }

        public static string Format(ConstructorInfo constructor)
        {
            return Format(constructor.ReflectedType) + ".ctor" + "(" + string.Join(", ", constructor.GetParameters().Select(parameter => Format(parameter.ParameterType)).ToArray()) + ")";
        }

        public static string Format(MethodInfo method)
        {
            if(ReferenceEquals(method.ReflectedType, null))
                return Format(method.ReturnType) + " " + FormatMethodWithoutParameters(method) + "(" + string.Join(", ", GetParameterTypes(method).Select(Format).ToArray()) + ")";
            return Format(method.ReturnType) + " " + Format(method.ReflectedType) + "." + FormatMethodWithoutParameters(method) + "(" + string.Join(", ", GetParameterTypes(method).Select(Format).ToArray()) + ")";
        }

        public static Func<MethodInfo, Type[]> BuildMethodBuilderParameterTypesExtractor()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(Type[]), new[] {typeof(MethodInfo)}, typeof(Formatter).Module, true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, typeof(MethodBuilder));
            var getParameterTypesMethod = typeof(MethodBuilder).GetMethod("GetParameterTypes", BindingFlags.Instance | BindingFlags.NonPublic);
            if(getParameterTypesMethod == null)
                throw new MissingMethodException(Format(typeof(MethodBuilder)), "GetParameterTypes");
            il.EmitCall(OpCodes.Callvirt, getParameterTypesMethod, null);
            il.Emit(OpCodes.Ret);
            return (Func<MethodInfo, Type[]>)method.CreateDelegate(typeof(Func<MethodInfo, Type[]>));
        }

        public static Func<ConstructorBuilder, Type[]> BuildConstructorBuilderParameterTypesExtractor()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(Type[]), new[] {typeof(ConstructorBuilder)}, typeof(Formatter).Module, true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            var getParameterTypesMethod = typeof(ConstructorBuilder).GetMethod("GetParameterTypes", BindingFlags.Instance | BindingFlags.NonPublic);
            if(getParameterTypesMethod == null)
                throw new MissingMethodException(Format(typeof(ConstructorBuilder)), "GetParameterTypes");
            il.EmitCall(OpCodes.Callvirt, getParameterTypesMethod, null);
            il.Emit(OpCodes.Ret);
            return (Func<ConstructorBuilder, Type[]>)method.CreateDelegate(typeof(Func<ConstructorBuilder, Type[]>));
        }

        public static Type[] GetParameterTypes(MethodInfo method)
        {
            // todo: обработать generic параметры
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

        private static string FormatMethodWithoutParameters(MethodInfo method)
        {
            if(!method.IsGenericMethod)
                return method.Name;
            return method.Name + "<" + string.Join(", ", method.GetGenericArguments().Select(Format).ToArray()) + ">";
        }

        private static Hashtable BuildParameterTypesExtractors()
        {
            var result = new Hashtable();
            var assembly = typeof(MethodInfo).Assembly;
            var runtimeMethodInfoType = assembly.GetTypes().FirstOrDefault(type => type.Name == "RuntimeMethodInfo");
            if(runtimeMethodInfoType == null)
                throw new InvalidOperationException("Type 'RuntimeMethodInfo' is not found");
            var methodBuilderInstantiationType = assembly.GetTypes().FirstOrDefault(type => type.Name == "MethodBuilderInstantiation");
            if(methodBuilderInstantiationType == null)
                throw new InvalidOperationException("Type 'MethodBuilderInstantiation' is not found");
            var methodOnTypeBuilderInstantiationType = assembly.GetTypes().FirstOrDefault(type => type.Name == "MethodOnTypeBuilderInstantiation");
            if(methodOnTypeBuilderInstantiationType == null)
                throw new InvalidOperationException("Type 'MethodOnTypeBuilderInstantiation' is not found");
            result[runtimeMethodInfoType] = result[typeof(DynamicMethod)] = (Func<MethodInfo, Type[]>)(method => method.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
            result[typeof(MethodBuilder)] = BuildMethodBuilderParameterTypesExtractor();
            result[methodBuilderInstantiationType] = BuildMethodBuilderInstantiationParameterTypesExtractor();
            result[methodOnTypeBuilderInstantiationType] = BuildMethodOnTypeBuilderInstantiationParametersTypeExtractor();
            return result;
        }

        private static Type[] Qzz(Type[] parameterTypes, Type declaringType, Type[] instantiation)
        {
            var dict = new Dictionary<Type, Type>();
            var index = 0;
            foreach(var type in declaringType.GetGenericArguments())
            {
                if(type.IsGenericParameter)
                    dict.Add(type, instantiation[index++]);
            }
            var result = new Type[parameterTypes.Length];
            for(var i = 0; i < parameterTypes.Length; ++i)
            {
                Type type;
                if(dict.TryGetValue(parameterTypes[i], out type))
                    result[i] = type;
                else result[i] = parameterTypes[i];
            }
            return result;
        }

        private static Func<MethodInfo, Type[]> BuildMethodOnTypeBuilderInstantiationParametersTypeExtractor()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(Type[]), new[] {typeof(MethodInfo)}, typeof(Formatter), true);
            var assembly = typeof(MethodInfo).Assembly;
            var methodOnTypeBuilderInstantiationType = assembly.GetTypes().FirstOrDefault(type => type.Name == "MethodOnTypeBuilderInstantiation");
            if(methodOnTypeBuilderInstantiationType == null)
                throw new InvalidOperationException("Type 'MethodOnTypeBuilderInstantiation' is not found");
            var typeBuilderInstantiationType = assembly.GetTypes().FirstOrDefault(type => type.Name == "TypeBuilderInstantiation");
            if(typeBuilderInstantiationType == null)
                throw new InvalidOperationException("Type 'TypeBuilderInstantiation' is not found");
            var methodField = methodOnTypeBuilderInstantiationType.GetField("m_method", BindingFlags.Instance | BindingFlags.NonPublic);
            if(methodField == null)
                throw new InvalidOperationException("Field 'MethodOnTypeBuilderInstantiation.m_method' is not found");
            var typeField = methodOnTypeBuilderInstantiationType.GetField("m_type", BindingFlags.Instance | BindingFlags.NonPublic);
            if(typeField == null)
                throw new InvalidOperationException("Field 'MethodOnTypeBuilderInstantiation.m_type' is not found");
            var typeTypeField = typeBuilderInstantiationType.GetField("m_type", BindingFlags.Instance | BindingFlags.NonPublic);
            if(typeTypeField == null)
                throw new InvalidOperationException("Field 'TypeBuilderInstantiation.m_type' is not found");
            var typeInstField = typeBuilderInstantiationType.GetField("m_inst", BindingFlags.Instance | BindingFlags.NonPublic);
            if(typeInstField == null)
                throw new InvalidOperationException("Field 'TypeBuilderInstantiation.m_inst' is not found");
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); // stack: [method]
            il.Emit(OpCodes.Castclass, methodOnTypeBuilderInstantiationType);
            il.Emit(OpCodes.Ldfld, methodField); // stack: [method.m_method]
            il.EmitCall(OpCodes.Call, HackHelpers.GetMethodDefinition<MethodInfo>(info => GetParameterTypes(info)), null); // stack: [GetParameterTypes(method.m_method)]
            il.Emit(OpCodes.Ldarg_0); // stack: [GetParameterTypes(method.m_method), method]
            il.Emit(OpCodes.Castclass, methodOnTypeBuilderInstantiationType);
            il.Emit(OpCodes.Ldfld, typeField); // stack: [GetParameterTypes(method.m_method), method.m_type]
            il.Emit(OpCodes.Castclass, typeBuilderInstantiationType);
            il.Emit(OpCodes.Ldfld, typeTypeField); // stack: [GetParameterTypes(method.m_method), method.m_type.m_type]
            il.Emit(OpCodes.Ldarg_0); // stack: [GetParameterTypes(method.m_method), method.m_type.m_type, method]
            il.Emit(OpCodes.Castclass, methodOnTypeBuilderInstantiationType);
            il.Emit(OpCodes.Ldfld, typeField); // stack: [GetParameterTypes(method.m_method), method.m_type.m_type, method.m_type]
            il.Emit(OpCodes.Castclass, typeBuilderInstantiationType);
            il.Emit(OpCodes.Ldfld, typeInstField); // stack: [GetParameterTypes(method.m_method), method.m_type.m_type, method.m_type.m_inst]
            il.EmitCall(OpCodes.Call, HackHelpers.GetMethodDefinition<Type[]>(types => Qzz(types, null, null)), null);
            il.Emit(OpCodes.Ret);
            return (Func<MethodInfo, Type[]>)method.CreateDelegate(typeof(Func<MethodInfo, Type[]>));
        }

        private static Type[] Zzz(Type[] parameterTypes, Type[] instantiation)
        {
            var index = 0;
            var result = new Type[parameterTypes.Length];
            for(var i = 0; i < parameterTypes.Length; i++)
            {
                var type = parameterTypes[i];
                result[i] = type.IsGenericParameter ? instantiation[index++] : type;
            }
            return result;
        }

        private static Func<MethodInfo, Type[]> BuildMethodBuilderInstantiationParameterTypesExtractor()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(Type[]), new[] {typeof(MethodInfo)}, typeof(Formatter), true);
            var assembly = typeof(MethodInfo).Assembly;
            var methodBuilderInstantiationType = assembly.GetTypes().FirstOrDefault(type => type.Name == "MethodBuilderInstantiation");
            if(methodBuilderInstantiationType == null)
                throw new InvalidOperationException("Type 'MethodBuilderInstantiation' is not found");
            var methodField = methodBuilderInstantiationType.GetField("m_method", BindingFlags.Instance | BindingFlags.NonPublic);
            if(methodField == null)
                throw new InvalidOperationException("Field 'MethodBuilderInstantiation.m_method' is not found");
            var instField = methodBuilderInstantiationType.GetField("m_inst", BindingFlags.Instance | BindingFlags.NonPublic);
            if(instField == null)
                throw new InvalidOperationException("Field 'MethodBuilderInstantiation.m_inst' is not found");
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); // stack: [method]
            il.Emit(OpCodes.Castclass, methodBuilderInstantiationType);
            il.Emit(OpCodes.Ldfld, methodField); // stack: [method.m_method]
            il.EmitCall(OpCodes.Call, HackHelpers.GetMethodDefinition<MethodInfo>(info => GetParameterTypes(info)), null); // stack: [GetParameterTypes(method.m_method)]
            il.Emit(OpCodes.Ldarg_0); // stack: [method.m_method.parameterTypes, method]
            il.Emit(OpCodes.Castclass, methodBuilderInstantiationType);
            il.Emit(OpCodes.Ldfld, instField); // stack: [method.m_method.parameterTypes, method.m_inst]
            il.EmitCall(OpCodes.Call, HackHelpers.GetMethodDefinition<Type[]>(types => Zzz(types, null)), null);
            il.Emit(OpCodes.Ret);
            return (Func<MethodInfo, Type[]>)method.CreateDelegate(typeof(Func<MethodInfo, Type[]>));
        }

        private static readonly Hashtable parameterTypesExtractors = BuildParameterTypesExtractors();
        private static readonly Func<ConstructorBuilder, Type[]> constructorBuilderParameterTypesExtractor = BuildConstructorBuilderParameterTypesExtractor();
    }
}