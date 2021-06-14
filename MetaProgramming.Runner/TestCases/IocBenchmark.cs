using MetaProgramming.Benchmark;
using System.Diagnostics;
using Ninject;
using StuctureMapContainer = StructureMap.Container;
using ReflectionIocContainer = MetaProgramming.IoC.Container;
using Autofac;
using Unity;

namespace MetaProgramming.Runner.TestCases
{
    public interface IGetSomeValueService
    {
        int GetValue();
    }

    public class GetSomeValueService : IGetSomeValueService
    {
        private readonly ILogger _logger;

        public GetSomeValueService(ILogger logger)
        {
            _logger = logger;
        }

        public int GetValue()
        {
            _logger.Log("Returning 12");
            return 12;
        }
    }

    public interface IIsValueEvenStrategy
    {
        bool IsEven(int value);
    }

    public class IsValueEvenStrategy : IIsValueEvenStrategy
    {
        private readonly ILogger _logger;

        public IsValueEvenStrategy(ILogger logger)
        {
            _logger = logger;
        }

        public bool IsEven(int value)
        {
            var isEven = value % 2 == 0;

            _logger.Log($"{value} is {(isEven ? "even" : "odd")}");

            return isEven;
        }

    }

    public interface ISomeService
    {
        int SomeValue { get; }

        bool SomeValueIsEven { get; }
    }

    public interface ILogger
    {
        void Log(string text);
    }

    public class IgnoreLogger : ILogger
    {
        public void Log(string text) { }
    }

    public class SomeService : ISomeService
    {
        public int SomeValue { get; }

        public bool SomeValueIsEven { get; }

        public SomeService(IGetSomeValueService getSomeValueService, IIsValueEvenStrategy isValueEvenStrategy)
        {
            SomeValue = getSomeValueService.GetValue();
            SomeValueIsEven = isValueEvenStrategy.IsEven(SomeValue);
        }
    }

    public class SomeServiceFactory
    {
        public ISomeService CreateSomeService()
        {
            var logger = new IgnoreLogger();
            return new SomeService(new GetSomeValueService(logger), new IsValueEvenStrategy(logger));
        }
    }

    public class IocBenchmark
    {
        private readonly SomeServiceFactory _simpleFactory;
        private readonly UnityContainer _unityContainer;
        private readonly StandardKernel _ninjectKernel;
        private readonly StuctureMapContainer _stuctureMapContainer;
        private readonly IContainer _autofacContainer;
        private readonly ReflectionIocContainer _reflectionIocContainer;

        public IocBenchmark()
        {
            _simpleFactory = new SomeServiceFactory();

            _unityContainer = new UnityContainer();
            _unityContainer.RegisterType<ISomeService, SomeService>();
            _unityContainer.RegisterType<IGetSomeValueService, GetSomeValueService>();
            _unityContainer.RegisterSingleton<IIsValueEvenStrategy, IsValueEvenStrategy>();
            _unityContainer.RegisterSingleton<ILogger, IgnoreLogger>();

            _ninjectKernel = new StandardKernel();
            _ninjectKernel.Bind<ISomeService>().To<SomeService>();
            _ninjectKernel.Bind<IGetSomeValueService>().To<GetSomeValueService>();
            _ninjectKernel.Bind<IIsValueEvenStrategy>().To<IsValueEvenStrategy>().InSingletonScope();
            _ninjectKernel.Bind<ILogger>().To<IgnoreLogger>().InSingletonScope();

            _stuctureMapContainer = new StuctureMapContainer();
            _stuctureMapContainer.Configure(config =>
            {
                config.For<ISomeService>().Use<SomeService>();
                config.For<IGetSomeValueService>().Use<GetSomeValueService>();
                config.For<IIsValueEvenStrategy>().Use<IsValueEvenStrategy>().Singleton();
                config.For<ILogger>().Use<IgnoreLogger>().Singleton();
            });

            var autofacBuilder = new ContainerBuilder();
            autofacBuilder.RegisterType<SomeService>().As<ISomeService>();
            autofacBuilder.RegisterType<GetSomeValueService>().As<IGetSomeValueService>();
            autofacBuilder.RegisterType<IsValueEvenStrategy>().As<IIsValueEvenStrategy>().SingleInstance();
            autofacBuilder.RegisterType<IgnoreLogger>().As<ILogger>().SingleInstance();

            _autofacContainer = autofacBuilder.Build();

            _reflectionIocContainer = new ReflectionIocContainer();
            _reflectionIocContainer.Transient<ISomeService, SomeService>();
            _reflectionIocContainer.Transient<IGetSomeValueService, GetSomeValueService>();
            _reflectionIocContainer.Singleton<IIsValueEvenStrategy, IsValueEvenStrategy>();
            _reflectionIocContainer.Singleton<ILogger, IgnoreLogger>();
        }

        [BenchmarkTest]
        [DefaultBenchmarkTest]
        public void NativeCtor()
        {
            var logger = new IgnoreLogger();
            new SomeService(new GetSomeValueService(logger), new IsValueEvenStrategy(logger));
        }

        [BenchmarkTest]
        public void SimpleFactory()
        {
            _simpleFactory.CreateSomeService();
        }

        [BenchmarkTest]
        public void StructureMapCreate()
        {
            _stuctureMapContainer.GetInstance<ISomeService>();
        }

        [BenchmarkTest]
        public void NinjectCreate()
        {
            _ninjectKernel.Get<ISomeService>();
        }

        [BenchmarkTest]
        public void UnityCreate()
        {
            _unityContainer.Resolve<ISomeService>();
        }

        [BenchmarkTest]
        public void AutofacCreate()
        {
            _autofacContainer.Resolve<ISomeService>();
        }

        [BenchmarkTest]
        public void ReflectionCreate()
        {
            _reflectionIocContainer.Get<ISomeService>();
        }

        public double ReflectionBruteForce()
        {
            ReflectionCreate();

            var sw = new Stopwatch();

            for (int i = 0; i < 5_000_000; i++)
            {
                sw.Start();
                ReflectionCreate();
                sw.Stop();
            }

            return (double)sw.Elapsed.TotalMilliseconds / 5.0;
        }
    }
}
