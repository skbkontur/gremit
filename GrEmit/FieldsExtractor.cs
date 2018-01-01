using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

namespace GrEmit
{
    public static class FieldsExtractor
    {
        public static Func<object, object> GetExtractor(FieldInfo field)
        {
            var extractor = (Func<object, object>)extractors[field];
            if(extractor == null)
            {
                lock(extractorsLock)
                {
                    extractor = (Func<object, object>)extractors[field];
                    if(extractor == null)
                        extractors[field] = extractor = BuildExtractor(field);
                }
            }
            return extractor;
        }

        public static Action<object, object> GetSetter(FieldInfo field)
        {
            var foister = (Action<object, object>)foisters[field];
            if(foister == null)
            {
                lock(foistersLock)
                {
                    foister = (Action<object, object>)foisters[field];
                    if(foister == null)
                        foisters[field] = foister = BuildFoister(field);
                }
            }
            return foister;
        }

        public static Func<T, TResult> GetExtractor<T, TResult>(FieldInfo field)
            where T : class
        {
            var extractor = GetExtractor(field);
            return arg => (TResult)extractor(arg);
        }

        public static Func<TResult> GetExtractor<TResult>(FieldInfo field)
        {
            var extractor = GetExtractor(field);
            return () => (TResult)extractor(null);
        }

        public static Action<T, TValue> GetSetter<T, TValue>(FieldInfo field)
            where T : class
        {
            var foister = GetSetter(field);
            return (inst, value) => foister(inst, value);
        }

        public static Action<TValue> GetSetter<TValue>(FieldInfo field)
        {
            var foister = GetSetter(field);
            return value => foister(null, value);
        }

        private static Func<object, object> BuildExtractor(FieldInfo field)
        {
            var methodName = "FieldExtractor$";
            if(field.IsStatic)
                methodName += field.DeclaringType + "$";
            methodName += field.Name + "$" + Guid.NewGuid();
            var dynamicMethod = new DynamicMethod(methodName, typeof(object), new[] {typeof(object)}, typeof(FieldsExtractor), true);
            using(var il = new GroboIL(dynamicMethod))
            {
                if(!field.IsStatic)
                {
                    il.Ldarg(0);
                    il.Castclass(field.DeclaringType);
                }
                il.Ldfld(field);
                if(field.FieldType.IsValueType)
                    il.Box(field.FieldType);
                il.Ret();
            }
            return (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>));
        }

        private static Action<object, object> BuildFoister(FieldInfo field)
        {
            var methodName = "FieldFoister$";
            if(field.IsStatic)
                methodName += field.DeclaringType + "$";
            methodName += field.Name + "$" + Guid.NewGuid();
            var dynamicMethod = new DynamicMethod(methodName, typeof(void), new[] {typeof(object), typeof(object)}, typeof(FieldsExtractor), true);
            using(var il = new GroboIL(dynamicMethod))
            {
                if(!field.IsStatic)
                {
                    il.Ldarg(0);
                    il.Castclass(field.DeclaringType);
                }
                il.Ldarg(1);
                if(field.FieldType.IsValueType)
                    il.Unbox_Any(field.FieldType);
                else
                    il.Castclass(field.FieldType);
                il.Stfld(field);
                il.Ret();
            }
            return (Action<object, object>)dynamicMethod.CreateDelegate(typeof(Action<object, object>));
        }

        private static readonly Hashtable extractors = new Hashtable();
        private static readonly object extractorsLock = new object();

        private static readonly Hashtable foisters = new Hashtable();
        private static readonly object foistersLock = new object();
    }
}