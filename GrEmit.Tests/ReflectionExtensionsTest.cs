using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using GrEmit.Utils;

using NUnit.Framework;

namespace GrEmit.Tests
{
    [TestFixture]
    public class ReflectionExtensionsTest
    {
        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
            module = assembly.DefineDynamicModule(Guid.NewGuid().ToString());
        }

        [Test]
        public void TestRuntimeMethodInfo_NonGenericClassNonGenericMethod()
        {
            var methodInfo = HackHelpers.GetMethodDefinition<NonGenericClass>(x => x.NonGenericMethod(0, null));
            var parameterTypes = ReflectionExtensions.GetParameterTypes(methodInfo);
            CollectionAssert.AreEqual(new[] { typeof(int), typeof(string) }, parameterTypes);
            Assert.AreEqual(typeof(string), ReflectionExtensions.GetReturnType(methodInfo));
        }

        [Test]
        public void TestRuntimeMethodInfo_NonGenericClassGenericMethod()
        {
            var methodInfo = HackHelpers.GetMethodDefinition<NonGenericClass>(x => x.GenericMethod<double, string>(0, 0, 0, null, 0, null, null));
            var parameterTypes = ReflectionExtensions.GetParameterTypes(methodInfo);
            CollectionAssert.AreEqual(new[] { typeof(double), typeof(int), typeof(double), typeof(List<string>), typeof(int), typeof(double[]), typeof(string) }, parameterTypes);
            Assert.AreEqual(typeof(List<string[]>), ReflectionExtensions.GetReturnType(methodInfo));
        }

        [Test]
        public void TestRuntimeMethodInfo_GenericClassNonGenericMethod()
        {
            var methodInfo = HackHelpers.GetMethodDefinition<GenericClass<double, string>>(x => x.NonGenericMethod(0, 0, null, null, 0, null));
            var parameterTypes = ReflectionExtensions.GetParameterTypes(methodInfo);
            CollectionAssert.AreEqual(new[] { typeof(double), typeof(int), typeof(List<string>), typeof(string), typeof(int), typeof(double[]) }, parameterTypes);
            Assert.AreEqual(typeof(List<string[]>), ReflectionExtensions.GetReturnType(methodInfo));
        }

        [Test]
        public void TestRuntimeMethodInfo_GenericClassGenericMethod()
        {
            var methodInfo = HackHelpers.GetMethodDefinition<GenericClass<double, string>>(x => x.GenericMethod<short, long>(0, 0, 0, null, 0, null, null, 0, 0, null, 0));
            var parameterTypes = ReflectionExtensions.GetParameterTypes(methodInfo);
            CollectionAssert.AreEqual(new[] { typeof(double), typeof(short), typeof(int), typeof(List<string>), typeof(short), typeof(string), typeof(List<long>), typeof(int), typeof(short), typeof(double[]), typeof(long) }, parameterTypes);
            Assert.AreEqual(typeof(List<Tuple<string[], long>>), ReflectionExtensions.GetReturnType(methodInfo));
        }

        [Test]
        public void TestRuntimeMethodInfo_NonGenericClassConstructor()
        {
            var methodInfo = HackHelpers.GetObjectConstruction(() => new NonGenericClass(0, null));
            var parameterTypes = ReflectionExtensions.GetParameterTypes(methodInfo);
            CollectionAssert.AreEqual(new[] { typeof(int), typeof(string) }, parameterTypes);
        }

        [Test]
        public void TestRuntimeMethodInfo_GenericClassConstructor()
        {
            var methodInfo = HackHelpers.GetObjectConstruction(() => new GenericClass<double, string>(0, 0, null, null, 0, null));
            var parameterTypes = ReflectionExtensions.GetParameterTypes(methodInfo);
            CollectionAssert.AreEqual(new[] { typeof(double), typeof(int), typeof(List<string>), typeof(string), typeof(int), typeof(double[]) }, parameterTypes);
        }

        [Test]
        public void TestMethodBuilder_NonGenericClassNonGenericMethod()
        {
            var typeBuilder = module.DefineType("NonGenericClass" + Guid.NewGuid(), TypeAttributes.Class | TypeAttributes.Public);
            var methodBuilder = typeBuilder.DefineMethod("NonGenericMethod", MethodAttributes.Public, typeof(string), new[] { typeof(int), typeof(string) });
            var parameterTypes = ReflectionExtensions.GetParameterTypes(methodBuilder);
            CollectionAssert.AreEqual(new[] { typeof(int), typeof(string) }, parameterTypes);
            Assert.AreEqual(typeof(string), ReflectionExtensions.GetReturnType(methodBuilder));
        }

        [Test]
        public void TestMethodBuilder_NonGenericClassGenericMethod()
        {
            var typeBuilder = module.DefineType("NonGenericClass" + Guid.NewGuid(), TypeAttributes.Class | TypeAttributes.Public);
            var methodBuilder = typeBuilder.DefineMethod("GenericMethod", MethodAttributes.Public);
            var genericParameters = methodBuilder.DefineGenericParameters("T1", "T2");
            var t1 = genericParameters[0];
            var t2 = genericParameters[1];
            // T1 x, int y, T1 a, List<T2> s, int c, T1[] d, T2 e
            methodBuilder.SetParameters(t1, typeof(int), t1, typeof(List<>).MakeGenericType(t2), typeof(int), t1.MakeArrayType(), t2);
            methodBuilder.SetReturnType(typeof(List<>).MakeGenericType(genericParameters[1].MakeArrayType()));
            var instantiatedMethod = methodBuilder.MakeGenericMethod(typeof(double), typeof(string));
            var parameterTypes = ReflectionExtensions.GetParameterTypes(instantiatedMethod);
            CollectionAssert.AreEqual(new[] { typeof(double), typeof(int), typeof(double), typeof(List<string>), typeof(int), typeof(double[]), typeof(string) }, parameterTypes);
            Assert.AreEqual(typeof(List<string[]>), ReflectionExtensions.GetReturnType(instantiatedMethod));
        }

        [Test]
        public void TestMethodBuilder_GenericClassNonGenericMethod()
        {
            var typeBuilder = module.DefineType("GenericClass" + Guid.NewGuid(), TypeAttributes.Class | TypeAttributes.Public);
            var genericParameters = typeBuilder.DefineGenericParameters("T1", "T2");
            var t1 = genericParameters[0];
            var t2 = genericParameters[1];
            // T1 x, int y, List<T2> s, T2 t, int z, T1[] q
            var methodBuilder = typeBuilder.DefineMethod("NonGenericMethod", MethodAttributes.Public, typeof(List<>).MakeGenericType(t2.MakeArrayType()), new[] { t1, typeof(int), typeof(List<>).MakeGenericType(t2), t2, typeof(int), t1.MakeArrayType() });
            var instantiatedMethod = TypeBuilder.GetMethod(typeBuilder.MakeGenericType(typeof(double), typeof(string)), methodBuilder);
            var parameterTypes = ReflectionExtensions.GetParameterTypes(instantiatedMethod);
            CollectionAssert.AreEqual(new[] { typeof(double), typeof(int), typeof(List<string>), typeof(string), typeof(int), typeof(double[]) }, parameterTypes);
            Assert.AreEqual(typeof(List<string[]>), ReflectionExtensions.GetReturnType(instantiatedMethod));
        }

        [Test]
        public void TestMethodBuilder_GenericClassGenericMethod()
        {
            var typeBuilder = module.DefineType("GenericClass" + Guid.NewGuid(), TypeAttributes.Class | TypeAttributes.Public);
            var typeGenericParameters = typeBuilder.DefineGenericParameters("T1", "T2");
            var t1 = typeGenericParameters[0];
            var t2 = typeGenericParameters[1];
            var methodBuilder = typeBuilder.DefineMethod("GenericMethod", MethodAttributes.Public);
            var methodGenericParameters = methodBuilder.DefineGenericParameters("T3", "T4");
            var t3 = methodGenericParameters[0];
            var t4 = methodGenericParameters[1];
            // T1 x, T3 a, int y, List<T2> s, T3 b, T2 t, List<T4> c, int z, T3 d, T1[] q, T4 e
            methodBuilder.SetParameters(t1, t3, typeof(int), typeof(List<>).MakeGenericType(t2), t3, t2, typeof(List<>).MakeGenericType(t4), typeof(int), t3, t1.MakeArrayType(), t4);
            methodBuilder.SetReturnType(typeof(List<>).MakeGenericType(typeof(Tuple<,>).MakeGenericType(t2.MakeArrayType(), t4)));
            var instantiatedMethod = TypeBuilder.GetMethod(typeBuilder.MakeGenericType(typeof(double), typeof(string)), methodBuilder).MakeGenericMethod(typeof(short), typeof(long));
            var parameterTypes = ReflectionExtensions.GetParameterTypes(instantiatedMethod);
            CollectionAssert.AreEqual(new[] { typeof(double), typeof(short), typeof(int), typeof(List<string>), typeof(short), typeof(string), typeof(List<long>), typeof(int), typeof(short), typeof(double[]), typeof(long) }, parameterTypes);
            Assert.AreEqual(typeof(List<Tuple<string[], long>>), ReflectionExtensions.GetReturnType(instantiatedMethod));
        }

        [Test]
        public void TestConstructorBuilder_NonGenericClass()
        {
            var typeBuilder = module.DefineType("NonGenericClass" + Guid.NewGuid(), TypeAttributes.Class | TypeAttributes.Public);
            var methodBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new[] { typeof(int), typeof(string) });
            var parameterTypes = ReflectionExtensions.GetParameterTypes(methodBuilder);
            CollectionAssert.AreEqual(new[] { typeof(int), typeof(string) }, parameterTypes);
        }

        [Test]
        public void TestConstructorBuilder_GenericClass()
        {
            var typeBuilder = module.DefineType("GenericClass" + Guid.NewGuid(), TypeAttributes.Class | TypeAttributes.Public);
            var genericParameters = typeBuilder.DefineGenericParameters("T1", "T2");
            var t1 = genericParameters[0];
            var t2 = genericParameters[1];
            // T1 x, int y, List<T2> s, T2 t, int z, T1[] q
            var methodBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new[] { t1, typeof(int), typeof(List<>).MakeGenericType(t2), t2, typeof(int), t1.MakeArrayType() });
            var instantiatedMethod = TypeBuilder.GetConstructor(typeBuilder.MakeGenericType(typeof(double), typeof(string)), methodBuilder);
            var parameterTypes = ReflectionExtensions.GetParameterTypes(instantiatedMethod);
            CollectionAssert.AreEqual(new[] { typeof(double), typeof(int), typeof(List<string>), typeof(string), typeof(int), typeof(double[]) }, parameterTypes);
        }

        [Test]
        public void TestRuntimeType()
        {
            Assert.AreEqual(typeof(C1<List<int>>), ReflectionExtensions.GetBaseType(typeof(C2<int, string>)));
            CollectionAssert.AreEquivalent(new[] { typeof(I3<int, double, string>), typeof(I2<int, double[]>), typeof(I1<List<int>>), typeof(I1<List<int[]>>) }, ReflectionExtensions.GetInterfaces(typeof(C3<int, double, string>)));
        }

        [Test]
        public void TestTypeBuilder_NonGeneric()
        {
            var c2 = module.DefineType("C2" + Guid.NewGuid(), TypeAttributes.Class | TypeAttributes.Public);
            c2.SetParent(typeof(C3<int, double, string>));
            c2.AddInterfaceImplementation(typeof(IEnumerable<int>));
            Assert.AreEqual(typeof(C3<int, double, string>), ReflectionExtensions.GetBaseType(c2));
            CollectionAssert.AreEquivalent(new[] { typeof(I3<int, double, string>), typeof(I2<int, double[]>), typeof(I1<List<int>>), typeof(I1<List<int[]>>), typeof(IEnumerable<int>) }, ReflectionExtensions.GetInterfaces(c2));
        }

        [Test]
        public void TestTypeBuilder_Generic_GetInterfaces()
        {
            var c3 = module.DefineType("C3" + Guid.NewGuid(), TypeAttributes.Class | TypeAttributes.Public);
            var genericParameters = c3.DefineGenericParameters("T1", "T2", "T3");
            var t1 = genericParameters[0];
            var t2 = genericParameters[1];
            var t3 = genericParameters[2];
            c3.SetParent(typeof(C3<,,>).MakeGenericType(t1, typeof(List<>).MakeGenericType(t2.MakeArrayType()), t3));
            var instantiatedType = c3.MakeGenericType(typeof(int), typeof(double), typeof(string));
            Assert.AreEqual(typeof(C3<int, List<double[]>, string>), ReflectionExtensions.GetBaseType(instantiatedType));
            Assert.IsTrue(ReflectionExtensions.IsAssignableFrom(instantiatedType, instantiatedType));
            Assert.IsTrue(ReflectionExtensions.IsAssignableFrom(c3, c3));
            Assert.IsTrue(ReflectionExtensions.IsAssignableFrom(c3.MakeArrayType(), c3.MakeArrayType()));
            CollectionAssert.AreEquivalent(new[] { typeof(I3<int, List<double[]>, string>), typeof(I2<int, List<double[]>[]>), typeof(I1<List<int>>), typeof(I1<List<int[]>>) }, ReflectionExtensions.GetInterfaces(instantiatedType));
        }

        public class NonGenericClass
        {
            public NonGenericClass(int x, string s)
            {
            }

            public string NonGenericMethod(int x, string s)
            {
                return null;
            }

            public List<T2[]> GenericMethod<T1, T2>(T1 x, int y, T1 a, List<T2> s, int c, T1[] d, T2 e)
            {
                return null;
            }
        }

        public class GenericClass<T1, T2>
        {
            public GenericClass(T1 x, int y, List<T2> s, T2 t, int z, T1[] q)
            {
            }

            public List<T2[]> NonGenericMethod(T1 x, int y, List<T2> s, T2 t, int z, T1[] q)
            {
                return null;
            }

            public List<Tuple<T2[], T4>> GenericMethod<T3, T4>(T1 x, T3 a, int y, List<T2> s, T3 b, T2 t, List<T4> c, int z, T3 d, T1[] q, T4 e)
            {
                return null;
            }
        }

        public interface I1<T>
        {
        }

        public interface I2<T1, T2> : I1<List<T1>>
        {
        }

        public interface I3<T1, T2, T3> : I2<T1, T2[]>
        {
        }

        public class C1<T> : I1<T>
        {
        }

        public class C2<T1, T2> : C1<List<T1>>
        {
        }

        public class C3<T1, T2, T3> : C2<T1[], T2>, I3<T1, T2, T3>
        {
        }

        private ModuleBuilder module;
    }
}