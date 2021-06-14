using MetaProgramming.Benchmark;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace MetaProgramming.Runner.TestCases
{
    public class Foo
    {
        internal readonly int _holyNumber = 42;

        public int HolyNumber
        {
            get
            {
                return _holyNumber;
            }
        }
    }

    public class ReflectionBenchmark
    {
        public int TotalSum = 0;

        private readonly Foo _foo;
        private readonly FieldInfo _field;
        private readonly MethodInfo _getMethod;
        private readonly PropertyInfo _propery;

        public ReflectionBenchmark()
        {
            _foo = new Foo();
            _propery = typeof(Foo).GetProperty(nameof(Foo.HolyNumber));
            _field = typeof(Foo).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(x => x.Name.ToLower().Contains("holynumber"));
            _getMethod = typeof(Foo).GetProperty(nameof(Foo.HolyNumber)).GetGetMethod();
        }

        [BenchmarkTest]
        [DefaultBenchmarkTest]
        public void NormalProperty()
        {
            TotalSum += _foo.HolyNumber;
        }

        [BenchmarkTest]
        public void BackingField()
        {
            TotalSum += _foo._holyNumber;
        }

        [BenchmarkTest]
        public void Reflection_GetValue()
        {
            TotalSum += (int)typeof(Foo).GetProperty(nameof(Foo.HolyNumber)).GetValue(_foo);
        }


        [BenchmarkTest]
        public void Reflection_GetValue_Cached()
        {
            TotalSum += (int)_propery.GetValue(_foo);
        }

        [BenchmarkTest]
        public void Reflection_GetMethod()
        {
            TotalSum += (int)typeof(Foo).GetProperty(nameof(Foo.HolyNumber)).GetGetMethod().Invoke(_foo, null);
        }

        [BenchmarkTest]
        public void Reflection_GetMethod_Cached()
        {
            TotalSum += (int)_getMethod.Invoke(_foo, null);
        }

        [BenchmarkTest]
        public void Reflection_GetField()
        {
            TotalSum += (int)typeof(Foo).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(x => x.Name.ToLower().Contains("holynumber")).GetValue(_foo);
        }

        [BenchmarkTest]
        public void Reflection_GetField_Cached()
        {
            TotalSum += (int)_field.GetValue(_foo);
        }
    }
}
