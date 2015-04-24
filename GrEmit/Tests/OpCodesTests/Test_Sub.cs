using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

using GrEmit;
using GrEmit.Utils;

using NUnit.Framework;

using System.Linq;

namespace Tests.OpCodesTests
{
    [TestFixture]
    public class Test_Sub
    {
        private void TestSuccess(Type type1, Type type2)
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] { type1, type2, }.Where(type => type != null).ToArray(), typeof(string), true);
            using (var il = new GroboIL(method))
            {
                int index = 0;
                if (type1 != null)
                    il.Ldarg(index++);
                else
                    il.Ldnull();
                if (type2 != null)
                    il.Ldarg(index++);
                else
                    il.Ldnull();
                il.Sub();
                il.Pop();
                il.Ret();
                Console.WriteLine(il.GetILCode());
            }
        }

        private void TestSuccess<T1, T2>()
        {
            TestSuccess(typeof(T1), typeof(T2));
        }

        private void TestFailure(Type type1, Type type2)
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] { type1, type2, }.Where(type => type != null).ToArray(), typeof(string), true);
            var il = new GroboIL(method);
            int index = 0;
            if (type1 != null)
                il.Ldarg(index++);
            else
                il.Ldnull();
            if (type2 != null)
                il.Ldarg(index++);
            else
                il.Ldnull();
            Assert.Throws<InvalidOperationException>(il.Sub);
        }

        private void TestFailure<T1, T2>()
        {
            TestFailure(typeof(T1), typeof(T2));
        }

        [Test]
        public void Test_int32_int32()
        {
            TestSuccess<int, int>();
        }

        [Test]
        public void Test_int32_int64()
        {
            TestFailure<int, long>();
        }

        [Test]
        public void Test_int32_native_int()
        {
            TestSuccess<int, IntPtr>();
        }

        [Test]
        public void Test_int32_float()
        {
            TestFailure<int, float>();
        }

        [Test]
        public void Test_int32_double()
        {
            TestFailure<int, double>();
        }

        [Test]
        public void Test_int32_O()
        {
            TestFailure<int, string>();
        }

        [Test]
        public void Test_int32_managed_pointer()
        {
            TestFailure(typeof(int), typeof(byte).MakeByRefType());
        }

        [Test]
        public void Test_int32_null()
        {
            TestSuccess(typeof(int), null);
        }

        [Test]
        public void Test_int64_int32()
        {
            TestFailure<long, int>();
        }

        [Test]
        public void Test_int64_int64()
        {
            TestSuccess<long, long>();
        }

        [Test]
        public void Test_int64_native_int()
        {
            TestFailure<long, IntPtr>();
        }

        [Test]
        public void Test_int64_float()
        {
            TestFailure<long, float>();
        }

        [Test]
        public void Test_int64_double()
        {
            TestFailure<long, double>();
        }

        [Test]
        public void Test_int64_O()
        {
            TestFailure<long, string>();
        }

        [Test]
        public void Test_int64_managed_pointer()
        {
            TestFailure(typeof(long), typeof(byte).MakeByRefType());
        }

        [Test]
        public void Test_int64_null()
        {
            TestSuccess(typeof(long), null);
        }

        [Test]
        public void Test_native_int_int32()
        {
            TestSuccess<IntPtr, int>();
        }

        [Test]
        public void Test_native_int_int64()
        {
            TestFailure<IntPtr, long>();
        }

        [Test]
        public void Test_native_int_native_int()
        {
            TestSuccess<IntPtr, IntPtr>();
        }

        [Test]
        public void Test_native_int_float()
        {
            TestFailure<IntPtr, float>();
        }

        [Test]
        public void Test_native_int_double()
        {
            TestFailure<IntPtr, double>();
        }

        [Test]
        public void Test_native_int_O()
        {
            TestFailure<IntPtr, string>();
        }

        [Test]
        public void Test_native_int_managed_pointer()
        {
            TestFailure(typeof(IntPtr), typeof(byte).MakeByRefType());
        }

        [Test]
        public void Test_native_int_null()
        {
            TestSuccess(typeof(IntPtr), null);
        }

        [Test]
        public void Test_float_int32()
        {
            TestFailure<float, int>();
        }

        [Test]
        public void Test_float_int64()
        {
            TestFailure<float, long>();
        }

        [Test]
        public void Test_float_native_int()
        {
            TestFailure<float, IntPtr>();
        }

        [Test]
        public void Test_float_float()
        {
            TestSuccess<float, float>();
        }

        [Test]
        public void Test_float_double()
        {
            TestSuccess<float, double>();
        }

        [Test]
        public void Test_float_O()
        {
            TestFailure<float, string>();
        }

        [Test]
        public void Test_float_managed_pointer()
        {
            TestFailure(typeof(float), typeof(byte).MakeByRefType());
        }

        [Test]
        public void Test_float_null()
        {
            TestSuccess(typeof(float), null);
        }

        [Test]
        public void Test_double_int32()
        {
            TestFailure<double, int>();
        }

        [Test]
        public void Test_double_int64()
        {
            TestFailure<double, long>();
        }

        [Test]
        public void Test_double_native_int()
        {
            TestFailure<double, IntPtr>();
        }

        [Test]
        public void Test_double_float()
        {
            TestSuccess<double, float>();
        }

        [Test]
        public void Test_double_double()
        {
            TestSuccess<double, double>();
        }

        [Test]
        public void Test_double_O()
        {
            TestFailure<double, string>();
        }

        [Test]
        public void Test_double_managed_pointer()
        {
            TestFailure(typeof(double), typeof(byte).MakeByRefType());
        }

        [Test]
        public void Test_double_null()
        {
            TestSuccess(typeof(double), null);
        }

        [Test]
        public void Test_managed_pointer_int32()
        {
            TestSuccess(typeof(byte).MakeByRefType(), typeof(int));
        }

        [Test]
        public void Test_managed_pointer_int64()
        {
            TestFailure(typeof(byte).MakeByRefType(), typeof(long));
        }

        [Test]
        public void Test_managed_pointer_native_int()
        {
            TestSuccess(typeof(byte).MakeByRefType(), typeof(IntPtr));
        }

        [Test]
        public void Test_managed_pointer_float()
        {
            TestFailure(typeof(byte).MakeByRefType(), typeof(float));
        }

        [Test]
        public void Test_managed_pointer_double()
        {
            TestFailure(typeof(byte).MakeByRefType(), typeof(double));
        }

        [Test]
        public void Test_managed_pointer_O()
        {
            TestFailure(typeof(byte).MakeByRefType(), typeof(string));
        }

        [Test]
        public void Test_managed_pointer_managed_pointer()
        {
            TestSuccess(typeof(byte).MakeByRefType(), typeof(byte).MakeByRefType());
        }

        [Test]
        public void Test_managed_pointer_null()
        {
            TestSuccess(typeof(byte).MakeByRefType(), null);
        }

        [Test]
        public void Test_O_int32()
        {
            TestFailure<string, int>();
        }

        [Test]
        public void Test_O_int64()
        {
            TestFailure<string, long>();
        }

        [Test]
        public void Test_O_native_int()
        {
            TestFailure<string, IntPtr>();
        }

        [Test]
        public void Test_O_float()
        {
            TestFailure<string, float>();
        }

        [Test]
        public void Test_O_double()
        {
            TestFailure<string, double>();
        }

        [Test]
        public void Test_O_O()
        {
            TestFailure<string, string>();
        }

        [Test]
        public void Test_O_managed_pointer()
        {
            TestFailure(typeof(string), typeof(byte).MakeByRefType());
        }

        [Test]
        public void Test_O_null()
        {
            TestFailure(typeof(string), null);
        }

        [Test]
        public void Test_null_int32()
        {
            TestSuccess(null, typeof(int));
        }

        [Test]
        public void Test_null_int64()
        {
            TestSuccess(null, typeof(long));
        }

        [Test]
        public void Test_null_native_int()
        {
            TestSuccess(null, typeof(IntPtr));
        }

        [Test]
        public void Test_null_float()
        {
            TestSuccess(null, typeof(float));
        }

        [Test]
        public void Test_null_double()
        {
            TestSuccess(null, typeof(double));
        }

        [Test]
        public void Test_null_O()
        {
            TestFailure(null, typeof(string));
        }

        [Test]
        public void Test_null_managed_pointer()
        {
            TestFailure(null, typeof(byte).MakeByRefType());
        }

        [Test]
        public void Test_null_null()
        {
            TestSuccess(null, null);
        }

        public static string[] Create(string[] values)
        {
            CheckDifferent(values);
            var hashSet = new HashSet<int>();
            for(var n = Math.Max(values.Length, 1);; ++n)
            {
                hashSet.Clear();
                var ok = true;
                foreach(var str in values)
                {
                    var idx = str.GetHashCode() % n;
                    if(idx < 0) idx += n;
                    if(hashSet.Contains(idx))
                    {
                        ok = false;
                        break;
                    }
                    hashSet.Add(idx);
                }
                if(ok)
                {
                    var result = new string[n];
                    foreach(var str in values)
                    {
                        var idx = str.GetHashCode() % n;
                        if(idx < 0) idx += n;
                        result[idx] = str;
                    }
                    return result;
                }
            }
        }

        private static void CheckDifferent(string[] values)
        {
            var hashSet = new HashSet<string>();
            foreach(var str in values)
            {
                if(hashSet.Contains(str))
                    throw new InvalidOperationException(string.Format("Duplicate value '{0}'", str));
                hashSet.Add(str);
            }
        }
    
        public interface IQxx
        {
            void Set(string key, int value);
        }

        const int numberOfCases = 5;

        private IQxx BuildIfs(ModuleBuilder module, string[] keys)
        {
            var typeBuilder = module.DefineType("Ifs", TypeAttributes.Class | TypeAttributes.Public);
            typeBuilder.AddInterfaceImplementation(typeof(IQxx));
            var fields = new FieldInfo[numberOfCases];
            for (int i = 0; i < numberOfCases; ++i)
                fields[i] = typeBuilder.DefineField(keys[i], typeof(int), FieldAttributes.Public);
            var method = typeBuilder.DefineMethod("Set", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), new[] { typeof(string), typeof(int) });
            method.DefineParameter(1, ParameterAttributes.In, "key");
            method.DefineParameter(2, ParameterAttributes.In, "value");
            using (var il = new GroboIL(method))
            {
                var doneLabel = il.DefineLabel("done");
                for (int i = 0; i < numberOfCases; ++i)
                {
                    il.Ldarg(1); // stack: [key]
                    il.Ldstr(keys[i]); // stack: [key, keys[i]]
                    il.Call(stringEqualityOperator); // stack: [key == keys[i]]
                    var nextKeyLabel = il.DefineLabel("nextKey");
                    il.Brfalse(nextKeyLabel); // if(key != keys[i]) goto nextKey; stack: []
                    il.Ldarg(0);
                    il.Ldarg(2);
                    il.Stfld(fields[i]);
                    il.Br(doneLabel);
                    il.MarkLabel(nextKeyLabel);
                }
                il.MarkLabel(doneLabel);
                il.Ret();
            }
            typeBuilder.DefineMethodOverride(method, typeof(IQxx).GetMethod("Set"));
            var type = typeBuilder.CreateType();
            return (IQxx)Activator.CreateInstance(type);
        }

        private IQxx BuildSwitch(ModuleBuilder module, string[] keys)
        {
            var typeBuilder = module.DefineType("Switch", TypeAttributes.Class | TypeAttributes.Public);
            typeBuilder.AddInterfaceImplementation(typeof(IQxx));
            var fields = new FieldInfo[numberOfCases];
            for (int i = 0; i < numberOfCases; ++i)
                fields[i] = typeBuilder.DefineField(keys[i], typeof(int), FieldAttributes.Public);
            var tinyHashtable = Create(keys);
            int n = tinyHashtable.Length;
            var keysField = typeBuilder.DefineField("keys", typeof(string[]), FieldAttributes.Public);
            var constructor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new[] { typeof(string[]) });
            using (var il = new GroboIL(constructor))
            {
                il.Ldarg(0);
                il.Ldarg(1);
                il.Stfld(keysField);
                il.Ret();
            }

            var method = typeBuilder.DefineMethod("Set", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), new[] { typeof(string), typeof(int) });
            method.DefineParameter(1, ParameterAttributes.In, "key");
            method.DefineParameter(2, ParameterAttributes.In, "value");
            using (var il = new GroboIL(method))
            {
                il.Ldarg(0);
                il.Ldfld(keysField);
                il.Ldarg(1);
                il.Call(HackHelpers.GetMethodDefinition<object>(o => o.GetHashCode()));
                il.Ldc_I4(n);
                il.Rem(true);
                var idx = il.DeclareLocal(typeof(int));
                il.Dup();
                il.Stloc(idx);
                il.Ldelem(typeof(string));
                il.Ldarg(1);
                il.Call(stringEqualityOperator);
                var doneLabel = il.DefineLabel("done");
                il.Brfalse(doneLabel);

                var labels = new GroboIL.Label[n];
                for (int i = 0; i < n; ++i)
                    labels[i] = doneLabel;
                foreach(string key in keys)
                {
                    var index = key.GetHashCode() % n;
                    if (index < 0) index += n;
                    var label = il.DefineLabel("set_" + key);
                    labels[index] = label;
                }
                il.Ldloc(idx);
                il.Switch(labels);
                for (int i = 0; i < keys.Length; ++i)
                {
                    var index = keys[i].GetHashCode() % n;
                    if (index < 0) index += n;
                    il.MarkLabel(labels[index]);
                    il.Ldarg(0);
                    il.Ldarg(2);
                    il.Stfld(fields[i]);
                    il.Br(doneLabel);
                }
                il.MarkLabel(doneLabel);
                il.Ret();
            }
            typeBuilder.DefineMethodOverride(method, typeof(IQxx).GetMethod("Set"));
            var type = typeBuilder.CreateType();
            return (IQxx)Activator.CreateInstance(type, new object[] { tinyHashtable });
        }

        [Test]
        public void TestPerformance_Ifs_vs_Switch()
        {
            var keys = new string[numberOfCases];
            for(int i = 0; i < numberOfCases; ++i)
                keys[i] = Guid.NewGuid().ToString();

            var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.RunAndSave);
            var module = assembly.DefineDynamicModule("Zzz", true);

            var instance = BuildIfs(module, keys);

            var stopwatch = Stopwatch.StartNew();
            const int iterations = 100000000;
            for(int iter = 0; iter < iterations / numberOfCases; ++iter)
            {
                for(int i = 0; i < numberOfCases; ++i)
                    instance.Set("zzz", iter);
                instance.Set("zzz", iter);
            }
            var elapsedIfs = stopwatch.Elapsed;
            Console.WriteLine("Ifs: " + elapsedIfs.TotalMilliseconds * 1000 / iterations + " microseconds (" + Math.Round(1000.0 * iterations / elapsedIfs.TotalMilliseconds) + " writes per second)");

            instance = BuildSwitch(module, keys);

            stopwatch = Stopwatch.StartNew();
            for(int iter = 0; iter < iterations / numberOfCases; ++iter)
            {
                for(int i = 0; i < numberOfCases; ++i)
                    instance.Set("zzz", iter);
                instance.Set("zzz", iter);
            }
            var elapsedSwitch = stopwatch.Elapsed;
            Console.WriteLine("Switch: " + elapsedSwitch.TotalMilliseconds * 1000 / iterations + " microseconds (" + Math.Round(1000.0 * iterations / elapsedSwitch.TotalMilliseconds) + " writes per second)");
            Console.WriteLine(elapsedSwitch.TotalMilliseconds / elapsedIfs.TotalMilliseconds);
        }

        private static readonly MethodInfo stringEqualityOperator = HackHelpers.GetMethodDefinition<string>(s => s == "");
    }
}