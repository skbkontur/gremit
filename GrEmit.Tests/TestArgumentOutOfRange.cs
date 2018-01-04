using System;
using System.Reflection.Emit;

using NUnit.Framework;

namespace GrEmit.Tests
{
    [TestFixture]
    public class TestArgumentOutOfRange
    {
        [Test]
        public void TestLdarg1()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(int), typeof(int)}, typeof(TestArgumentOutOfRange));
            var il = new GroboIL(method);
            il.Ldarg(0);
            il.Ldarg(1);
            Assert.Throws<ArgumentOutOfRangeException>(() => il.Ldarg(-2));
            Assert.Throws<ArgumentOutOfRangeException>(() => il.Ldarg(2));
        }

        [Test]
        public void TestLdarg1_WithDisposableWrapper()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(int), typeof(int)}, typeof(TestArgumentOutOfRange));
                    using(var il = new GroboIL(method))
                    {
                        il.Ldarg(0);
                        il.Ldarg(1);
                        il.Ldarg(-2);
                    }
                });
        }

        [Test]
        public void TestLdarg2()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(int), typeof(int)}, typeof(TestArgumentOutOfRange));
            var il = new GroboIL(method);
            il.Ldarg(0);
            il.Ldarg(1);
            Assert.Throws<ArgumentOutOfRangeException>(() => il.Ldarg(2));
        }

        [Test]
        public void TestStarg1()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(int), typeof(int)}, typeof(TestArgumentOutOfRange));
            var il = new GroboIL(method);
            il.Ldc_I4(0);
            il.Starg(0);
            il.Ldc_I4(0);
            il.Starg(1);
            il.Ldc_I4(0);
            Assert.Throws<ArgumentOutOfRangeException>(() => il.Starg(-2));
        }

        [Test]
        public void TestStarg2()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(int), typeof(int)}, typeof(TestArgumentOutOfRange));
            var il = new GroboIL(method);
            il.Ldc_I4(0);
            il.Starg(0);
            il.Ldc_I4(0);
            il.Starg(1);
            il.Ldc_I4(0);
            Assert.Throws<ArgumentOutOfRangeException>(() => il.Starg(2));
        }

        [Test]
        public void TestLdarga1()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(int), typeof(int)}, typeof(TestArgumentOutOfRange));
            var il = new GroboIL(method);
            il.Ldarga(0);
            il.Ldarga(1);
            Assert.Throws<ArgumentOutOfRangeException>(() => il.Ldarga(-2));
        }

        [Test]
        public void TestLdarga2()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(int), typeof(int)}, typeof(TestArgumentOutOfRange));
            var il = new GroboIL(method);
            il.Ldarga(0);
            il.Ldarga(1);
            Assert.Throws<ArgumentOutOfRangeException>(() => il.Ldarga(2));
        }
    }
}