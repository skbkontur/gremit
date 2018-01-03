using System;
using System.Linq;
using System.Reflection.Emit;

using NUnit.Framework;

namespace GrEmit.Tests.OpCodesTests
{
    [TestFixture]
    public class Test_Neg
    {
        [Test]
        public void Test_int32()
        {
            TestSuccess<int>();
        }

        [Test]
        public void Test_int64()
        {
            TestSuccess<long>();
        }

        [Test]
        public void Test_native_int()
        {
            TestSuccess<IntPtr>();
        }

        [Test]
        public void Test_float()
        {
            TestSuccess<float>();
        }

        [Test]
        public void Test_double()
        {
            TestSuccess<double>();
        }

        [Test]
        public void Test_O()
        {
            TestFailure<string>();
        }

        [Test]
        public void Test_managed_pointer()
        {
            TestFailure(typeof(byte).MakeByRefType());
        }

        [Test]
        public void Test_null()
        {
            TestSuccess(null);
        }

        private void TestSuccess(Type type)
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {type}.Where(t => t != null).ToArray(), typeof(string), true);
            using(var il = new GroboIL(method))
            {
                var index = 0;
                if(type != null)
                    il.Ldarg(index++);
                else
                    il.Ldnull();
                il.Neg();
                il.Pop();
                il.Ret();
                Console.WriteLine(il.GetILCode());
            }
        }

        private void TestSuccess<T>()
        {
            TestSuccess(typeof(T));
        }

        private void TestFailure(Type type)
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {type}.Where(t => t != null).ToArray(), typeof(string), true);
            var il = new GroboIL(method);
            var index = 0;
            if(type != null)
                il.Ldarg(index++);
            else
                il.Ldnull();
            Assert.Throws<InvalidOperationException>(il.Neg);
        }

        private void TestFailure<T>()
        {
            TestFailure(typeof(T));
        }
    }
}