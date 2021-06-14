using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace MetaProgramming.Benchmark
{
    public class BenchmarkTestAttribute : Attribute { }

    public class DefaultBenchmarkTestAttribute : Attribute { }

    public enum TimeScale
    {
        Ns,
        Ms,
    }

    public static class BenchmarkRunner
    {
        private class BenchmarkResult
        {
            public BenchmarkResult(string functionName, TimeSpan duration)
            {
                FunctionName = functionName;
                Duration = duration;
            }

            public string FunctionName { get; }

            public TimeSpan Duration { get; }
        }

        public static void RunBenchmark<T>(Action<string> printLine, int repetitions = 1_000, TimeScale timeScale = TimeScale.Ns)
        {
            var classWithBenchmarks = typeof(T);

            var benchmarkFunctions =
                classWithBenchmarks.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.IsDefined(typeof(BenchmarkTestAttribute), true))
                .Where(x => x.ReturnType == typeof(void));

            var defaultBenchmarkName = benchmarkFunctions.FirstOrDefault(x => x.IsDefined(typeof(DefaultBenchmarkTestAttribute), true))?.Name;

            T instanceWithBenchmark = (T)Activator.CreateInstance(classWithBenchmarks);

            var results = new List<BenchmarkResult>();

            foreach (var function in benchmarkFunctions)
            {
                TimeSpan stopExecutionDuration(T x)
                {
                    var sw = Stopwatch.StartNew();
                    function.Invoke(x, null);
                    sw.Stop();
                    return sw.Elapsed;
                }

                Func<T, TimeSpan> stopAction = SomeForeshadowing<T>(function);

                var discardWarmUp = stopAction(instanceWithBenchmark);

                var ellapsed = TimeSpan.Zero;
                for (int i = 0; i < Math.Max(repetitions, 1); i++)
                    ellapsed += stopAction(instanceWithBenchmark);

                results.Add(new BenchmarkResult(function.Name, ellapsed));
            }

            var longestName = results.OrderByDescending(x => x.FunctionName.Length).First().FunctionName;

            var defaultOrMaxTotal =
                results.FirstOrDefault(x => x.FunctionName == defaultBenchmarkName)?.Duration
                ?? results.OrderByDescending(x => x.Duration).First().Duration;

            foreach (var result in results.OrderBy(x => x.Duration))
            {
                var percent = result.Duration.Ticks / (double)defaultOrMaxTotal.Ticks * 100;

                var factorTotalNs = (decimal)repetitions;
                var unit = "";
                switch (timeScale)
                {
                    case TimeScale.Ms:
                        factorTotalNs /= (decimal)1;
                        unit = "ms";
                        break;
                    case TimeScale.Ns:
                        factorTotalNs /= (decimal)1_000_000;
                        unit = "ns";
                        break;
                    default:
                        break;
                }

                var average = (decimal)result.Duration.TotalMilliseconds / factorTotalNs;

                printLine($"{result.FunctionName.PadRight(longestName.Length)} : Average {average,10:N2} {unit} {percent,10:N2}%");
            }

            if (instanceWithBenchmark is IDisposable disposable)
                disposable.Dispose();
        }

        private static Func<T, TimeSpan> SomeForeshadowing<T>(MethodInfo info)
        {
            var dm = new DynamicMethod("Test_" + Guid.NewGuid().ToString(), typeof(TimeSpan), new Type[] { typeof(T) }, typeof(BenchmarkRunner).Module);

            var swCtor = typeof(Stopwatch).GetConstructors().First(x => x.GetParameters().Length == 0);

            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Newobj, swCtor);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Call, typeof(Stopwatch).GetMethod(nameof(Stopwatch.Start)));
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, info);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Call, typeof(Stopwatch).GetMethod(nameof(Stopwatch.Stop)));
            il.Emit(OpCodes.Call, typeof(Stopwatch).GetProperty(nameof(Stopwatch.Elapsed)).GetGetMethod(true));
            il.Emit(OpCodes.Ret);

            return ((Func<T, TimeSpan>)dm.CreateDelegate(typeof(Func<T, TimeSpan>)));
        }
    }
}
