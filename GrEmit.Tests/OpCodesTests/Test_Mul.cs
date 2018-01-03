using System;
using System.Linq;
using System.Reflection.Emit;

using NUnit.Framework;

namespace GrEmit.Tests.OpCodesTests
{
    [TestFixture]
    public class Test_Mul
    {
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
            TestFailure(typeof(byte).MakeByRefType(), typeof(int));
        }

        [Test]
        public void Test_managed_pointer_int64()
        {
            TestFailure(typeof(byte).MakeByRefType(), typeof(long));
        }

        [Test]
        public void Test_managed_pointer_native_int()
        {
            TestFailure(typeof(byte).MakeByRefType(), typeof(IntPtr));
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
            TestFailure(typeof(byte).MakeByRefType(), typeof(byte).MakeByRefType());
        }

        [Test]
        public void Test_managed_pointer_null()
        {
            TestFailure(typeof(byte).MakeByRefType(), null);
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

        private void TestSuccess(Type type1, Type type2)
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {type1, type2,}.Where(type => type != null).ToArray(), typeof(string), true);
            using(var il = new GroboIL(method))
            {
                var index = 0;
                if(type1 != null)
                    il.Ldarg(index++);
                else
                    il.Ldnull();
                if(type2 != null)
                    il.Ldarg(index++);
                else
                    il.Ldnull();
                il.Mul();
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
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {type1, type2,}.Where(type => type != null).ToArray(), typeof(string), true);
            var il = new GroboIL(method);
            var index = 0;
            if(type1 != null)
                il.Ldarg(index++);
            else
                il.Ldnull();
            if(type2 != null)
                il.Ldarg(index++);
            else
                il.Ldnull();
            Assert.Throws<InvalidOperationException>(il.Mul);
        }

        private void TestFailure<T1, T2>()
        {
            TestFailure(typeof(T1), typeof(T2));
        }
    }
}