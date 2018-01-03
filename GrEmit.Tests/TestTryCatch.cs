using System;
#if NET45
using System.Reflection;
using System.Threading;
#endif
using System.Reflection.Emit;

using NUnit.Framework;

namespace GrEmit.Tests
{
    [TestFixture]
    public class TestTryCatch
    {
        [Test]
        public void Test1()
        {
            var overflow = typeof(OverflowException);
            var exCtorInfo = overflow.GetConstructor(new[] { typeof(string) });
            var exToStrMI = overflow.GetMethod("ToString");
            var writeLineMI = typeof(Console).GetMethod("WriteLine",
                                                               new[]
                                                                   {
                                                                       typeof(string),
                                                                       typeof(object)
                                                                   });

            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(int), new[] { typeof(int), typeof(int) }, typeof(TestTryCatch));

            using (var il = new GroboIL(method))
            {
                GroboIL.Local tmp1 = il.DeclareLocal(typeof(int));
                GroboIL.Local tmp2 = il.DeclareLocal(overflow);

                // In order to successfully branch, we need to create labels
                // representing the offset IL instruction block to branch to.
                // These labels, when the MarkLabel(Label) method is invoked,
                // will specify the IL instruction to branch to.
                //
                GroboIL.Label failed = il.DefineLabel("failed");
                GroboIL.Label endOfMthd = il.DefineLabel("end");

                // Begin the try block.
                il.BeginExceptionBlock();

                // First, load argument 0 and the integer value of "100" onto the
                // stack. If arg0 > 100, branch to the label "failed", which is marked
                // as the address of the block that throws an exception.
                //
                il.Ldarg(0);
                il.Ldc_I4(100);
                il.Bgt(failed, false);

                // Now, check to see if argument 1 was greater than 100. If it was,
                // branch to "failed." Otherwise, fall through and perform the addition,
                // branching unconditionally to the instruction at the label "endOfMthd".
                //
                il.Ldarg(1);
                il.Ldc_I4(100);
                il.Bgt(failed, false);

                il.Ldarg(0);
                il.Ldarg(1);
                il.Add_Ovf(true);
                // Store the result of the addition.
                il.Stloc(tmp1);
                il.Leave(endOfMthd);

                // If one of the arguments was greater than 100, we need to throw an
                // exception. We'll use "OverflowException" with a customized message.
                // First, we load our message onto the stack, and then create a new
                // exception object using the constructor overload that accepts a
                // string message.
                //
                il.MarkLabel(failed);
                il.Ldstr("Cannot accept values over 100 for add.");
                il.Newobj(exCtorInfo);

                // We're going to need to refer to that exception object later, so let's
                // store it in a temporary variable. Since the store function pops the
                // the value/reference off the stack, and we'll need it to throw the
                // exception, we will subsequently load it back onto the stack as well.

                il.Stloc(tmp2);
                il.Ldloc(tmp2);

                // Throw the exception now on the stack.

                il.Throw();

                // Start the catch block for OverflowException.
                //
                il.BeginCatchBlock(overflow);

                // When we enter the catch block, the thrown exception 
                // is on the stack. Store it, then load the format string
                // for WriteLine. 
                //
                il.Stloc(tmp2);
                il.Ldstr("Caught {0}");

                // Push the thrown exception back on the stack, then 
                // call its ToString() method. Note that if this catch block
                // were for a more general exception type, like Exception,
                // it would be necessary to use the ToString for that type.
                //
                il.Ldloc(tmp2);
                il.Call(exToStrMI);

                // The format string and the return value from ToString() are
                // now on the stack. Call WriteLine(string, object).
                //
                il.Call(writeLineMI);

                // Since our function has to return an integer value, we'll load -1 onto
                // the stack to indicate an error, and store it in local variable tmp1.
                //
                il.Ldc_I4(-1);
                il.Stloc(tmp1);

                // End the exception handling block.

                il.EndExceptionBlock();

                // The end of the method. If no exception was thrown, the correct value
                // will be saved in tmp1. If an exception was thrown, tmp1 will be equal
                // to -1. Either way, we'll load the value of tmp1 onto the stack and return.
                //
                il.MarkLabel(endOfMthd);
                il.Ldloc(tmp1);
                il.Ret();

                Console.WriteLine(il.GetILCode());
            }
        }

#if NET45
        [Test]
        public void Test2()
        {
            AssemblyName myAssemblyName = new AssemblyName();
            myAssemblyName.Name = "AdderExceptionAsm";

            // Create dynamic assembly.
            AppDomain myAppDomain = Thread.GetDomain();
            AssemblyBuilder myAssemblyBuilder = myAppDomain.DefineDynamicAssembly(myAssemblyName, AssemblyBuilderAccess.RunAndSave);

            // Create a dynamic module.
            ModuleBuilder myModuleBuilder = myAssemblyBuilder.DefineDynamicModule("AdderExceptionMod", true);
            var symbolDocumentWriter = myModuleBuilder.GetSymWriter().DefineDocument("AdderException.cil", Guid.Empty, Guid.Empty, Guid.Empty);

            TypeBuilder myTypeBuilder = myModuleBuilder.DefineType("Adder");
            Type[] adderParams = { typeof(int), typeof(int) };

            ConstructorInfo myConstructorInfo = typeof(OverflowException).GetConstructor(new[] { typeof(string) });
            MethodInfo myExToStrMI = typeof(OverflowException).GetMethod("ToString");
            MethodInfo myWriteLineMI = typeof(Console).GetMethod("WriteLine", new[] { typeof(string), typeof(object) });

            // Define method to add two numbers.
            MethodBuilder myMethodBuilder = myTypeBuilder.DefineMethod("DoAdd", MethodAttributes.Public |
               MethodAttributes.Static, typeof(int), adderParams);
            using (var il = new GroboIL(myMethodBuilder, symbolDocumentWriter))
            {

                // Declare local variable.
                GroboIL.Local myLocalBuilder1 = il.DeclareLocal(typeof(int));
                GroboIL.Local myLocalBuilder2 = il.DeclareLocal(typeof(OverflowException));

                // Define label.
                GroboIL.Label myFailedLabel = il.DefineLabel("failed");
                GroboIL.Label myEndOfMethodLabel = il.DefineLabel("end");

                // Begin exception block.
                il.BeginExceptionBlock();

                il.Ldarg(0);
                il.Ldc_I4(10);
                il.Bgt(myFailedLabel, false);

                il.Ldarg(1);
                il.Ldc_I4(10);
                il.Bgt(myFailedLabel, false);

                il.Ldarg(0);
                il.Ldarg(1);
                il.Add_Ovf(true);
                il.Stloc(myLocalBuilder1);
                il.Leave(myEndOfMethodLabel);

                il.MarkLabel(myFailedLabel);
                il.Ldstr("Cannot accept values over 10 for add.");
                il.Newobj(myConstructorInfo);

                il.Stloc(myLocalBuilder2);
                il.Ldloc(myLocalBuilder2);

                // Throw the exception.
                il.Throw();

                // Call 'BeginExceptFilterBlock'.
                il.BeginExceptFilterBlock();
                il.WriteLine("Except filter block called.");

                // Call catch block.
                il.BeginCatchBlock(null);

                // Call other catch block.
                il.BeginCatchBlock(typeof(OverflowException));

                il.Ldstr("{0}");
                il.Ldloc(myLocalBuilder2);
                il.Call(myExToStrMI);
                il.Call(myWriteLineMI);
                il.Ldc_I4(-1);
                il.Stloc(myLocalBuilder1);

                // Call finally block.
                il.BeginFinallyBlock();
                il.WriteLine("Finally block called.");

                // End the exception block.
                il.EndExceptionBlock();

                il.MarkLabel(myEndOfMethodLabel);
                il.Ldloc(myLocalBuilder1);
                il.Ret();

                Console.WriteLine(il.GetILCode());
            }
        }
#endif
    }
}