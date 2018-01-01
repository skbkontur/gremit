using System;
using System.Reflection;
using System.Reflection.Emit;

namespace GrEmit.MethodBodyParsing
{
    internal class DynamicScope
    {
        static DynamicScope()
        {
            var assembly = typeof(DynamicMethod).Assembly;
            dynamicScopeType = assembly.GetType("System.Reflection.Emit.DynamicScope");
            varArgsMethodType = assembly.GetType("System.Reflection.Emit.VarArgMethod");
            runtimeMethodInfoType = assembly.GetType("System.Reflection.RuntimeMethodInfo");

            itemGetter = BuildItemGetter();
            getTokenFor = BuildGetTokenFor();
        }

        public DynamicScope(object inst)
        {
            this.inst = inst;
        }

        private static Func<object, int, object> BuildItemGetter()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(object), new[] {typeof(object), typeof(int)}, typeof(string), true);
            using(var il = new GroboIL(method))
            {
                il.Ldarg(0); // stack: [scope]
                il.Castclass(dynamicScopeType);
                il.Ldarg(1); // stack: [scope, token]
                var property = dynamicScopeType.GetProperty("Item", BindingFlags.Instance | BindingFlags.NonPublic);
                if(property == null)
                    throw new MissingMethodException("DynamicScope", "get_Item");
                var getter = property.GetGetMethod(true);
                il.Call(getter); // stack: [scope[this]]
                il.Ret();
            }

            return (Func<object, int, object>)method.CreateDelegate(typeof(Func<object, int, object>));
        }

        private static Func<object, MethodBase, SignatureHelper, uint> BuildGetTokenFor()
        {
            var parameterTypes = new[] {typeof(object), typeof(MethodBase), typeof(SignatureHelper)};
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(uint), parameterTypes, typeof(string), true);
            using(var il = new GroboIL(method))
            {
                il.Ldarg(0); // stack: [scope]
                il.Castclass(dynamicScopeType);
                il.Ldarg(1); // stack: [scope, method]
                il.Castclass(runtimeMethodInfoType); // stack: [scope, (RuntimeMethodInfo)method]
                il.Ldarg(2); // stack: [scope, (RuntimeMethodInfo)method, signature]

                var constructor = varArgsMethodType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] {runtimeMethodInfoType, typeof(SignatureHelper)}, null);
                if(constructor == null)
                    throw new MissingMethodException("VarArgsMethod", ".ctor");
                var getTokenForMethod = dynamicScopeType.GetMethod("GetTokenFor", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] {varArgsMethodType}, null);
                if(getTokenForMethod == null)
                    throw new MissingMethodException("DynamicScope", "GetTokenFor");

                il.Newobj(constructor); // stack: [scope, new VarArgsMethod((RuntimeMethodInfo)method, signature)]
                il.Call(getTokenForMethod); // stack: [scope.GetTokenFor(new VarArgsMethod((RuntimeMethodInfo)method, signature))]

                il.Ret();
            }

            return (Func<object, MethodBase, SignatureHelper, uint>)method.CreateDelegate(typeof(Func<object, MethodBase, SignatureHelper, uint>));
        }

        public object this[int token] => itemGetter(inst, token);

        public MetadataToken GetTokenFor(MethodBase method, SignatureHelper signature)
        {
            return new MetadataToken(getTokenFor(inst, method, signature));
        }

        public static void Init()
        {
        }

        public readonly object inst;

        private static readonly Func<object, int, object> itemGetter;
        private static readonly Func<object, MethodBase, SignatureHelper, uint> getTokenFor;
        private static readonly Type dynamicScopeType;
        private static readonly Type varArgsMethodType;
        private static readonly Type runtimeMethodInfoType;
    }
}