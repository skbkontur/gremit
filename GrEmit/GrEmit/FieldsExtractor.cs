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
                lock(lockObject)
                {
                    extractor = (Func<object, object>)extractors[field];
                    if(extractor == null)
                        extractors[field] = extractor = BuildExtractor(field);
                }
            }
            return extractor;
        }

        public static Func<T, TResult> GetExtractor<T, TResult>(FieldInfo field)
            where T : class
            where TResult : class
        {
            var extractor = GetExtractor(field);
            return arg => (TResult)extractor(arg);
        }

        private static Func<object, object> BuildExtractor(FieldInfo field)
        {
            var dynamicMethod = new DynamicMethod("FieldExtractor$" + field.Name + "$" + Guid.NewGuid(), typeof(object), new[] {typeof(object)}, typeof(FieldsExtractor), true);
            using(var il = new GroboIL(dynamicMethod))
            {
                il.Ldarg(0);
                il.Castclass(field.DeclaringType);
                il.Ldfld(field);
                if(field.FieldType.IsValueType)
                    il.Box(field.FieldType);
                il.Ret();
            }
            return (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>));
        }

        private static readonly Hashtable extractors = new Hashtable();
        private static readonly object lockObject = new object();
    }
}