using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

namespace GrEmit
{
    public static class FieldsExtractor
    {
        public static Func<T, TResult> GetExtractor<T, TResult>(FieldInfo field)
            where T : class
            where TResult : class
        {
            var extractor = (Func<T, TResult>)extractors[field];
            if(extractor == null)
            {
                lock(lockObject)
                {
                    extractor = (Func<T, TResult>)extractors[field];
                    if(extractor == null)
                        extractors[field] = extractor = BuildExtractor<T, TResult>(field);
                }
            }
            return extractor;
        }

        private static Func<T, TResult> BuildExtractor<T, TResult>(FieldInfo field)
        {
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(TResult), new[] {typeof(T)}, typeof(FieldsExtractor), true);
            using(var il = new GroboIL(dynamicMethod))
            {
                il.Ldarg(0);
                il.Castclass(field.DeclaringType);
                il.Ldfld(field);
                il.Castclass(typeof(TResult));
                il.Ret();
            }
            return (Func<T, TResult>)dynamicMethod.CreateDelegate(typeof(Func<T, TResult>));
        }

        private static readonly Hashtable extractors = new Hashtable();
        private static readonly object lockObject = new object();
    }
}