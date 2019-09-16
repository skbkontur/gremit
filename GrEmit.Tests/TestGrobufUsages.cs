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
            using (var il = new GroboIL(method))
            {
                il.Ldarga(0); // stack: [obj]
                il.Ldfld(GetDateTimeOffsetField("_dateTime", "m_dateTime")); // stack: [obj.m_dateTime]

                il.Ldarga(0); // stack: [obj]
                il.Ldfld(GetDateTimeOffsetField("_offsetMinutes", "m_offsetMinutes")); // stack: [obj.m_offsetMinutes]

                il.Call(assertMethod); // stack: []

                il.Ret();
            }

            return method;
        }

        // DateTimeOffset field names depend on framework (Netcore vs Net45) AND mono version (mono 6 vs earlier)
        private FieldInfo GetDateTimeOffsetField(string firstName, string secondName)
        {
            return dateTimeOffsetType.GetField(firstName, BindingFlags.Instance | BindingFlags.NonPublic)
                   ?? dateTimeOffsetType.GetField(secondName, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private static readonly Type dateTimeOffsetType = typeof(DateTimeOffset);
        private static readonly DateTimeOffset dateTimeOffset = new DateTimeOffset(new DateTime(2018, 01, 05, 21, 34, 56, DateTimeKind.Unspecified), TimeSpan.FromMinutes(15));
    }
}