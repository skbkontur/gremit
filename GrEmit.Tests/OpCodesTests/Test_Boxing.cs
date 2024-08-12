using System;
using System.Reflection.Emit;

using NUnit.Framework;

namespace GrEmit.Tests.OpCodesTests
{
    [TestFixture]
    public class Test_Boxing
    {
        [Test]
        public void TestUnboxInt_Success()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(int)});
            var il = new GroboIL(method);

            var valueTypeLocal = il.DeclareLocal(typeof(int).MakeByRefType());
            
            il.Ldc_I4(15);
            il.Box(typeof(int));
            il.Unbox(typeof(int));
            il.Stloc(valueTypeLocal);
            
            il.Ret();
            Console.WriteLine(il.GetILCode());
        }
        
        // Fails because a string cannot be unboxed.
        [Test]
        public void TestUnboxString_Failure()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(int)});
            var il = new GroboIL(method);
            
            il.Ldstr("Test String");
            Assert.Throws<ArgumentException>(() => il.Unbox(typeof(string)));
        }
        
        // Fails because the wrong type is on the stack
        [Test]
        public void TestUnboxWrong_Stack()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(int)});
            var il = new GroboIL(method);

            il.Ldc_I4(0);
            Assert.Throws<InvalidOperationException>(() => il.Unbox(typeof(int)));
        }
        
        // Fails because a string cannot be boxed.
        [Test]
        public void TestBoxString_Failure()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(int)});
            var il = new GroboIL(method);

            il.Ldstr("Test String");
            Assert.Throws<ArgumentException>(() => il.Box(typeof(string)));
            il.Pop();
            
            il.Ret();
            Console.WriteLine(il.GetILCode());
        }
    }
}