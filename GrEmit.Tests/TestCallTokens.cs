using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using NUnit.Framework;

namespace GrEmit.Tests
{
    [TestFixture]
    public class TestCallTokens
    {
        [Test]
        public void TestStaticCall()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(int), new[] {typeof(int), typeof(int)}, typeof(TestCallTokens), true);
            using (var il = new GroboIL(method))
            {
                il.Ldarg(0);
                il.Ldarg(1);
                il.Call(typeof(Math).GetMethod("Max", new[] {typeof(int), typeof(int)}));
                il.Ret();
            }
            var func = (Func<int, int, int>)method.CreateDelegate(typeof(Func<int, int, int>));
            Assert.AreEqual(7, func(3, 7));
            AssertNoVarArgTokens(method);
        }

        [Test]
        public void TestVirtualAndNonVirtualCalls()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(string), new[] {typeof(object), typeof(object)}, typeof(TestCallTokens), true);
            using (var il = new GroboIL(method))
            {
                il.Ldarg(0);
                il.Call(typeof(object).GetMethod("ToString")); // callvirt
                il.Ldarg(1);
                il.Callnonvirt(typeof(object).GetMethod("ToString")); // call
                il.Call(typeof(string).GetMethod("Concat", new[] {typeof(string), typeof(string)}));
                il.Ret();
            }
            var func = (Func<object, object, string>)method.CreateDelegate(typeof(Func<object, object, string>));
            Assert.AreEqual("42System.Object", func(42, new object()));
            AssertNoVarArgTokens(method);
        }

        [Test]
        public void TestGenericMethodAndMethodOfGenericType()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(int), new[] {typeof(List<int>), typeof(int[])}, typeof(TestCallTokens), true);
            using (var il = new GroboIL(method))
            {
                il.Ldarg(0);
                il.Ldc_I4(5);
                il.Call(typeof(List<int>).GetMethod("Add")); // method of generic type
                il.Ldarg(1);
                il.Call(typeof(Enumerable).GetMethods().Single(m => m.Name == "Sum" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(IEnumerable<int>))); // non-generic overload
                il.Ldarg(0);
                il.Call(typeof(Enumerable).GetMethods().Single(m => m.Name == "Count" && m.GetParameters().Length == 1).MakeGenericMethod(typeof(int))); // generic method instantiation
                il.Add();
                il.Ret();
            }
            var func = (Func<List<int>, int[], int>)method.CreateDelegate(typeof(Func<List<int>, int[], int>));
            var list = new List<int> {1};
            Assert.AreEqual(1 + 2 + 3 + 2, func(list, new[] {1, 2, 3}));
            Assert.AreEqual(new[] {1, 5}, list.ToArray());
            AssertNoVarArgTokens(method);
        }

        [Test]
        public void TestMultiDimensionalArrayMethod()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(int), new[] {typeof(int[,])}, typeof(TestCallTokens), true);
            using (var il = new GroboIL(method))
            {
                il.Ldarg(0);
                il.Ldc_I4(1);
                il.Ldc_I4(0);
                il.Call(typeof(int[,]).GetMethod("Get"));
                il.Ret();
            }
            var func = (Func<int[,], int>)method.CreateDelegate(typeof(Func<int[,], int>));
            Assert.AreEqual(3, func(new[,] {{1, 2}, {3, 4}}));
            AssertNoVarArgTokens(method);
        }

        [Test]
        public void TestCallOfAnotherDynamicMethod()
        {
            var inner = new DynamicMethod(Guid.NewGuid().ToString(), typeof(int), new[] {typeof(int)}, typeof(TestCallTokens), true);
            using (var il = new GroboIL(inner))
            {
                il.Ldarg(0);
                il.Ldc_I4(10);
                il.Mul();
                il.Ret();
            }
            var outer = new DynamicMethod(Guid.NewGuid().ToString(), typeof(int), new[] {typeof(int)}, typeof(TestCallTokens), true);
            using (var il = new GroboIL(outer))
            {
                il.Ldarg(0);
                il.Call(inner);
                il.Ldc_I4(1);
                il.Add();
                il.Ret();
            }
            var func = (Func<int, int>)outer.CreateDelegate(typeof(Func<int, int>));
            Assert.AreEqual(41, func(4));
            AssertNoVarArgTokens(outer);
        }

        [Test]
        public void TestCallIntoMethodBuilderStillWorks()
        {
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
            var module = assembly.DefineDynamicModule(Guid.NewGuid().ToString());
            var typeBuilder = module.DefineType("CallTokensType", TypeAttributes.Public | TypeAttributes.Class);
            var methodBuilder = typeBuilder.DefineMethod("Twice", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new[] {typeof(int)});
            using (var il = new GroboIL(methodBuilder))
            {
                il.Ldarg(0);
                il.Ldarg(0);
                il.Add();
                il.Ret();
            }
            var caller = typeBuilder.DefineMethod("Caller", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new[] {typeof(int)});
            using (var il = new GroboIL(caller))
            {
                il.Ldarg(0);
                il.Call(methodBuilder);
                il.Ldc_I4(1);
                il.Call(typeof(Math).GetMethod("Max", new[] {typeof(int), typeof(int)})); // runtime method from MethodBuilder IL
                il.Ret();
            }
            var type = typeBuilder.CreateTypeInfo();
            Assert.AreEqual(8, type.GetMethod("Caller").Invoke(null, new object[] {4}));
            Assert.AreEqual(1, type.GetMethod("Caller").Invoke(null, new object[] {0}));
        }

        private static void AssertNoVarArgTokens(DynamicMethod method)
        {
            // Internal layout of DynamicMethod/DynamicILGenerator/DynamicScope; skip the check if it is not available on this runtime.
            var ilGenerator = method.GetILGenerator();
            var scope = ilGenerator.GetType().GetField("m_scope", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(ilGenerator);
            var tokens = scope?.GetType().GetField("m_tokens", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(scope) as IEnumerable;
            if (tokens == null)
                Assert.Ignore("DynamicScope internals are not available on this runtime");
            var varArgTokens = tokens.Cast<object>().Count(token => token != null && token.GetType().Name == "VarArgMethod");
            Assert.AreEqual(0, varArgTokens);
        }
    }
}