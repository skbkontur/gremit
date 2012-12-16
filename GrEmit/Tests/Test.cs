using System;
using System.Reflection.Emit;

using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class Test
    {
        [Test]
        public void TestRet()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), Type.EmptyTypes, typeof(Test));
            var il = new GrEmit.GroboIL(method);
            il.Ret();
            Console.Write(il.GetILCode());
        }

        [Test]
        public void TestAPlusB()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(int), new[] {typeof(int), typeof(int)}, typeof(Test));
            var il = new GrEmit.GroboIL(method);
            il.Ldarg(0);
            il.Ldarg(1);
            il.Add();
            il.Ret();
            Console.Write(il.GetILCode());
        }

        [Test]
        public void TestHelloWorld()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(string), Type.EmptyTypes, typeof(Test));
            var il = new GrEmit.GroboIL(method);
            il.Ldstr("Hello World");
            il.Ret();
            Console.Write(il.GetILCode());
        }

        [Test]
        public void TestMax()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(int), new[] { typeof(int), typeof(int) }, typeof(Test));
            var il = new GrEmit.GroboIL(method);
            il.Ldarg(0);
            il.Ldarg(1);
            var returnSecondLabel = il.DefineLabel("returnSecond");
            il.Ble(typeof(int), returnSecondLabel);
            il.Ldarg(0);
            il.Ret();
            il.MarkLabel(returnSecondLabel);
            il.Ldarg(1);
            il.Ret();
            Console.Write(il.GetILCode());
        }

        [Test]
        public void TestFarsh()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(int), new[] { typeof(int) }, typeof(Test));
            var il = new GrEmit.GroboIL(method);
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
            il.Bgt(typeof(int), label2); // if(temp > 0) goto L_2; stack: [cur]
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
}