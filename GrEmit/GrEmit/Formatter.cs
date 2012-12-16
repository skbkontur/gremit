using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace GrEmit
{
    public static class Formatter
    {
        public static string Format(Type type)
        {
            if(!type.IsGenericType)
                return type.Name;
            return type.Name + "<" + string.Join(", ", type.GetGenericArguments().Select(Format)) + ">";
        }

        public static string Format(FieldInfo field)
        {
            return Format(field.ReflectedType) + "." + field.Name;
        }

        public static string Format(ConstructorInfo constructor)
        {
            return Format(constructor.ReflectedType) + ".ctor" + "(" + string.Join(", ", constructor.GetParameters().Select(parameter => Format(parameter.ParameterType))) + ")";
        }

        public static string Format(MethodInfo method)
        {
            if(ReferenceEquals(method.ReflectedType, null))
                return Format(method.ReturnType) + " " + method.Name + "(" + string.Join(", ", GetParameterTypes(method).Select(Format)) + ")";
            return Format(method.ReturnType) + " " + Format(method.ReflectedType) + "." + method.Name + "(" + string.Join(", ", GetParameterTypes(method).Select(Format)) + ")";
        }

        public static Func<MethodBuilder, Type[]> BuildMethodBuilderParameterTypesExtractor()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(Type[]), new[] {typeof(MethodBuilder)}, typeof(Formatter).Module, true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            var getParameterTypesMethod = typeof(MethodBuilder).GetMethod("GetParameterTypes", BindingFlags.Instance | BindingFlags.NonPublic);
            if(getParameterTypesMethod == null)
                throw new MissingMethodException(Format(typeof(MethodBuilder)), "GetParameterTypes");
            il.EmitCall(OpCodes.Callvirt, getParameterTypesMethod, null);
            il.Emit(OpCodes.Ret);
            return (Func<MethodBuilder, Type[]>)method.CreateDelegate(typeof(Func<MethodBuilder, Type[]>));
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
            if(method is MethodBuilder)
                return methodBuilderParameterTypesExtractor((MethodBuilder)method);
            return method.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
        }

        public static Type[] GetParameterTypes(ConstructorInfo constructor)
        {
            if(constructor is ConstructorBuilder)
                return constructorBuilderParameterTypesExtractor((ConstructorBuilder)constructor);
            return constructor.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
        }

        private static readonly Func<MethodBuilder, Type[]> methodBuilderParameterTypesExtractor = BuildMethodBuilderParameterTypesExtractor();
        private static readonly Func<ConstructorBuilder, Type[]> constructorBuilderParameterTypesExtractor = BuildConstructorBuilderParameterTypesExtractor();
    }
}