using System;
using System.Reflection.Emit;

using NUnit.Framework;

namespace GrEmit.Tests
{
    [TestFixture]
    public class TestLabels
    {
        [Test]
        public void TestLabelHasNotBeenMarked()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), Type.EmptyTypes, typeof(string), true);
            var il = new GroboIL(method);
            var label = il.DefineLabel("L");
            il.Ldc_I4(0);
            il.Brfalse(label);
            il.Ret();
            var e = Assert.Throws<InvalidOperationException>(il.Seal);
            Assert.AreEqual("The label 'L_0' has not been marked", e.Message);
        }

        [Test]
        public void TestLabelHasNotBeenMarked_Switch()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), Type.EmptyTypes, typeof(string), true);
            var il = new GroboIL(method);
            var label = il.DefineLabel("L");
            il.Ldc_I4(0);
            il.Switch(label);
            il.Ret();
            var e = Assert.Throws<InvalidOperationException>(il.Seal);
            Assert.AreEqual("The label 'L_0' has not been marked", e.Message);
        }

        [Test]
        public void TestLabelHasNotBeenUsed()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), Type.EmptyTypes, typeof(string), true);
            using(var il = new GroboIL(method))
            {
                il.DefineLabel("L");
                il.Ret();
            }
        }

        [Test]
        public void TestLabelHasBeenMarkedTwice()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), Type.EmptyTypes, typeof(string), true);
            var il = new GroboIL(method);
            var label = il.DefineLabel("L");
            il.Ldc_I4(0);
            il.Brfalse(label);
            il.Ldc_I4(1);
            il.Pop();
            il.MarkLabel(label);
            il.Ldc_I4(2);
            il.Pop();
            var e = Assert.Throws<InvalidOperationException>(() => il.MarkLabel(label));
            Assert.AreEqual("The label 'L_0' has already been marked", e.Message);
        }
    }
}