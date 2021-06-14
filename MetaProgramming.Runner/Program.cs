using System;
using MetaProgramming.Benchmark;
using MetaProgramming.Runner.TestCases;
using MetaProgramming.ORM.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Linq;

namespace MetaProgramming.Runner
{
    public enum TestCase
    {
        Benchmark,
        Ioc,
        Orm,
        Controller,
    }

    class Program
    {
        static void Main(string[] args)
        {
            var testToRun = TestCase.Benchmark;

            switch (testToRun)
            {
                case TestCase.Benchmark:
                    BenchmarkRunner.RunBenchmark<ReflectionBenchmark>(Console.WriteLine, 100_000);
                    break;

                case TestCase.Ioc:
                    BenchmarkRunner.RunBenchmark<IocBenchmark>(Console.WriteLine, 100_000);
                    // var nss = new IocBenchmark().ReflectionBruteForce();
                    // Console.WriteLine();
                    // Console.WriteLine($"Reflection_BruteForce {nss:n2} ns");
                    break;

                case TestCase.Orm:
                    using (var connection = new SqlConnection(BloggingContext.ConnectionString))
                    {
                        connection.Open();
                        var command = connection.CreateCommand();
                        command.CommandText = "DELETE FROM Blogs;";
                        command.ExecuteNonQuery();
                    }

                    BenchmarkRunner.RunBenchmark<OrmBenchmark>(Console.WriteLine, 100, TimeScale.Ms);
                    break;

                case TestCase.Controller:
                    using (var c = new Controller.RootController())
                    {
                        c.Start();
                        Console.WriteLine("Listening");
                    
                        Console.ReadKey();
                    
                        c.Stop();
                    }
                    Console.WriteLine("Test 2");

                    break;
            }

            Console.ReadLine();
        }
    }
}
