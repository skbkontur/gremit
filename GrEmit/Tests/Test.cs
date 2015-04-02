using System;
using System.Reflection.Emit;

using GrEmit;
using GrEmit.Utils;

using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class Test
    {
        struct Qxx
        {
            private Guid X1;
            private Guid X2;
            private Guid X3;
            private Guid X4;
            private Guid X5;
            private Guid X6;
            private Guid X7;
            private Guid X8;
            private Guid X9;
        }

        [Test]
        public void TestDifferentStructs()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(int)}, typeof(Test));
            var il = new GroboIL(method);
            var loc1 = il.DeclareLocal(typeof(int?));
            var loc2 = il.DeclareLocal(typeof(Qxx?));
            var label1 = il.DefineLabel("zzz");
            il.Ldarg(0);
            il.Brfalse(label1);
            il.Ldloc(loc1);
            var label2 = il.DefineLabel("qxx");
            il.Br(label2);
            il.MarkLabel(label1);
            il.Ldloc(loc2);
            Assert.Throws<InvalidOperationException>(() => il.MarkLabel(label2));
        }

        [Test]
        public void TestRet()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), Type.EmptyTypes, typeof(Test));
            using (var il = new GroboIL(method))
            {
                il.Ret();
                Console.Write(il.GetILCode());
            }
        }

        [Test]
        public void TestAPlusB()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(int), new[] {typeof(int), typeof(int)}, typeof(Test));
            using (var il = new GroboIL(method))
            {
                il.Ldarg(0);
                il.Ldarg(1);
                il.Add();
                il.Ret();
                Console.Write(il.GetILCode());
            }
        }

        [Test]
        public void TestZ()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(int), new[] {typeof(bool), typeof(float), typeof(double)}, typeof(Test));
            using (var il = new GroboIL(method))
            {
                il.Ldarg(0);
                var label1 = il.DefineLabel("label");
                il.Brfalse(label1);
                il.Ldarg(1);
                var label2 = il.DefineLabel("label");
                il.Br(label2);
                il.MarkLabel(label1);
                il.Ldarg(2);
                il.MarkLabel(label2);
                il.Ldc_I4(1);
                il.Conv<float>();
                il.Add();
                il.Conv<int>();
                il.Ret();
                Console.Write(il.GetILCode());
            }
        }


        private class A
        {
            
        }

        private class B : A
        {
        }

        private class C : A
        {
        }

        [Test]
        public void TestZ2()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(A), new[] {typeof(bool), typeof(B), typeof(C)}, typeof(Test));
            using (var il = new GroboIL(method))
            {
                il.Ldarg(0);
                var label1 = il.DefineLabel("label");
                il.Brfalse(label1);
                il.Ldarg(1);
                var label2 = il.DefineLabel("label");
                il.Br(label2);
                il.MarkLabel(label1);
                il.Ldarg(2);
                il.MarkLabel(label2);
                il.Ret();
                Console.Write(il.GetILCode());
            }
        }

        [Test]
        public void TestHelloWorld()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(string), Type.EmptyTypes, typeof(Test));
            using (var il = new GroboIL(method))
            {
                il.Ldstr("Hello World");
                il.Ret();
                Console.Write(il.GetILCode());
            }
        }

        [Test]
        public void TestMax()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(int), new[] { typeof(int), typeof(int) }, typeof(Test));
            using (var il = new GroboIL(method))
            {
                il.Ldarg(0);
                il.Ldarg(1);
                var returnSecondLabel = il.DefineLabel("returnSecond");
                il.Ble(returnSecondLabel, false);
                il.Ldarg(0);
                il.Ret();
                il.MarkLabel(returnSecondLabel);
                il.Ldarg(1);
                il.Ret();
                Console.Write(il.GetILCode());
            }
        }

        [Test]
        public void TestPrefixes()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(int), new[] { typeof(IntPtr), typeof(int) }, typeof(Test));
            using (var il = new GroboIL(method))
            {
                il.Ldarg(0);
                il.Ldarg(1);
                il.Add();
                il.Ldind(typeof(int));
                il.Ret();
                Console.Write(il.GetILCode());
            }
        }

        [Test]
        public void TestFarsh()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(int), new[] { typeof(int) }, typeof(Test));
            using (var il = new GroboIL(method))
            {
                var temp = il.DeclareLocal(typeof(int));
                il.Ldarg(0); // stack: [x]
                var label0 = il.DefineLabel("L");
                il.Br(label0); // goto L_0; stack: [x]

                il.Ldstr("zzz");
                il.Ldobj(typeof(DateTime));
                il.Mul();
                il.Initobj(typeof(int));

                var label1 = il.DefineLabel("L");
                il.MarkLabel(label1); // stack: [x, 2]
                il.Stloc(temp); // temp = 2; stack: [x]
                var label2 = il.DefineLabel("L");
                il.MarkLabel(label2); // stack: [cur]
                il.Ldarg(0); // stack: [cur, x]
                il.Mul(); // stack: [cur * x = cur]
                il.Ldloc(temp); // stack: [cur, temp]
                il.Ldc_I4(1); // stack: [cur, temp, 1]
                il.Sub(); // stack: [cur, temp - 1]
                il.Stloc(temp); // temp = temp - 1; stack: [cur]
                il.Ldloc(temp); // stack: [cur, temp]
                il.Ldc_I4(0); // stack: [cur, temp, 0]
                il.Bgt(label2, false); // if(temp > 0) goto L_2; stack: [cur]
                var label3 = il.DefineLabel("L");
                il.Br(label3); // goto L_3; stack: [cur]
                il.MarkLabel(label0); // stack: [x]
                il.Ldc_I4(2); // stack: [x, 2]
                il.Br(label1); // goto L_1; stack: [x, 2]
                il.MarkLabel(label3); // stack: [cur]
                il.Ret(); // return cur; stack: []
                Console.Write(il.GetILCode());
            }
        }


        private class Zzz
        {
            public Zzz(int x)
            {
                X = x;
            }

            public int X { get; private set; }
        }

        [Test]
        public void TestConstructorCall()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(Zzz)}, typeof(Test));
            using (var il = new GroboIL(method))
            {
                il.Ldarg(0);
                il.Ldc_I4(8);
                il.Call(typeof(Zzz).GetConstructor(new[] {typeof(int)}));
                il.Ret();
                Console.Write(il.GetILCode());
            }
            var action = (Action<Zzz>)method.CreateDelegate(typeof(Action<Zzz>));
            var zzz = new Zzz(3);
            action(zzz);
            Assert.AreEqual(8, zzz.X);
        }

        public interface I1
        {
        }

        public interface I2
        {
        }

        public class C1 : I1, I2
        {
        }

        public class C2 : I1, I2
        {
        }

        public static void F1(I1 i1)
        {
        }

        public static void F2(I2 i2)
        {
        }

        [Test]
        public void TestDifferentPaths()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(bool), typeof(C1), typeof(C2)}, typeof(string), true);
            var il = new GroboIL(method);
            il.Ldarg(0);
            var label1 = il.DefineLabel("L1");
            il.Brfalse(label1);
            il.Ldarg(1);
            var label2 = il.DefineLabel("L2");
            il.Br(label2);
            il.MarkLabel(label1);
            il.Ldarg(2);
            il.MarkLabel(label2);
            il.Dup();
            il.Call(HackHelpers.GetMethodDefinition<I1>(x => F1(x)));
            il.Call(HackHelpers.GetMethodDefinition<I2>(x => F2(x)));
            il.Ret();
            var action = method.CreateDelegate(typeof(Action<bool, C1, C2>));
        }

    }
}