using System;
using System.Reflection;
using System.Reflection.Emit;

using NUnit.Framework;

namespace GrEmit.Tests
{
    public class TestGrobufUsages
    {
        [Test]
        public void DateTimeOffsetPrivateFieldsAccess()
        {
            var method = BuildAccessorMethod();
            var @delegate = (Action<DateTimeOffset>)method.CreateDelegate(typeof(Action<DateTimeOffset>));
            @delegate(dateTimeOffset);
        }

        private DynamicMethod BuildAccessorMethod()
        {
            var assertMethod = typeof(TestGrobufUsages).GetMethod("AssertDateTimeOffsetFields", BindingFlags.Static | BindingFlags.NonPublic);

            var method = new DynamicMethod("Grobuf_Write_DateTimeOffset_" + Guid.NewGuid(), typeof(void), new[] {dateTimeOffsetType}, typeof(TestGrobufUsages), true);
            using(var il = new GroboIL(method))
            {
                il.Ldarga(0); // stack: [obj]
                il.Ldfld(dateTimeOffsetType.GetField(SelectName("_dateTime", "m_dateTime"), BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [obj.m_dateTime]

                il.Ldarga(0); // stack: [obj]
                il.Ldfld(dateTimeOffsetType.GetField(SelectName("_offsetMinutes", "m_offsetMinutes"), BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [obj.m_offsetMinutes]

                il.Call(assertMethod); // stack: []

                il.Ret();
            }

            return method;
        }

        private static void AssertDateTimeOffsetFields(DateTime dateTime, short offsetMinutes)
        {
            Assert.That(dateTime, Is.EqualTo(new DateTime(2018, 01, 05, 21, 19, 56, DateTimeKind.Unspecified)));
            Assert.That(offsetMinutes, Is.EqualTo(15));
        }

        private static string SelectName(string netcoreName, string net45Name)
        {
#if NETCOREAPP2_0
            return netcoreName;
#else
            return net45Name;
#endif
        }

        private readonly Type dateTimeOffsetType = typeof(DateTimeOffset);
        private static readonly DateTimeOffset dateTimeOffset = new DateTimeOffset(new DateTime(2018, 01, 05, 21, 34, 56, DateTimeKind.Unspecified), TimeSpan.FromMinutes(15));
    }
}