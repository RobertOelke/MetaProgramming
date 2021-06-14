using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace MetaProgramming.IoC
{
    public class Container
    {
        private enum Scope
        {
            AlwaysUnique,
            Transient,
            Singleton,
        }

        private class CreationContext
        {
            public ConcurrentDictionary<Type, (Scope scope, object creator)> RegisteredTypes;

            public ConcurrentDictionary<Type, object> Singletons;

            public ConcurrentDictionary<Type, object> Transients = new ConcurrentDictionary<Type, object>();

            public CreationContext(ConcurrentDictionary<Type, (Scope scope, object creator)> registeredTypes, ConcurrentDictionary<Type, object> singletons)
            {
                RegisteredTypes = registeredTypes;
                Singletons = singletons;
                Transients = new ConcurrentDictionary<Type, object>();
            }
        }

        private static Func<CreationContext, TImplementation> CreateFunction<TImplementation>()
        {
            var typeToCreate = typeof(TImplementation);
            var dm = new DynamicMethod($"Create_{typeToCreate.Name}_{Guid.NewGuid()}", typeToCreate, new Type[] { typeof(CreationContext) }, typeof(Container).Module);
            var greedyCtor = typeToCreate.GetConstructors().OrderBy(x => x.GetParameters().Length).Last();

            var il = dm.GetILGenerator();
            
            foreach (var param in greedyCtor.GetParameters())
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, typeof(Container).GetMethod(nameof(CreateInstance), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(param.ParameterType));
            }

            il.Emit(OpCodes.Newobj, greedyCtor);
            il.Emit(OpCodes.Ret);

            return (Func<CreationContext, TImplementation>)dm.CreateDelegate(typeof(Func<CreationContext, TImplementation>));
        }

        private static TImplementation CreateInstance<TImplementation>(CreationContext context)
        {
            if (context.RegisteredTypes.TryGetValue(typeof(TImplementation), out (Scope scope, object creator) tuple))
            {
                switch (tuple.scope)
                {
                    case Scope.Transient:
                        return (TImplementation)context.Transients.GetOrAdd(typeof(TImplementation), t => ((Func<CreationContext, TImplementation>)tuple.creator).Invoke(context));
                    case Scope.Singleton:
                        return (TImplementation)context.Singletons.GetOrAdd(typeof(TImplementation), t => ((Func<CreationContext, TImplementation>)tuple.creator).Invoke(context));
                    default:
                        return ((Func<CreationContext, TImplementation>)tuple.creator).Invoke(context);
                }
            }

            throw new ArgumentException(typeof(TImplementation).Name);
        }

        private readonly ConcurrentDictionary<Type, (Scope scope, object creator)> _registeredTypes = new ConcurrentDictionary<Type, (Scope scope, object creator)>();

        private readonly ConcurrentDictionary<Type, object> _createdSingletons = new ConcurrentDictionary<Type, object>();

        public void Transient<TInterface, TImplementation>() =>
            _registeredTypes.TryAdd(typeof(TInterface), (Scope.Transient, CreateFunction<TImplementation>()));

        public void Singleton<TInterface, TImplementation>() => 
            _registeredTypes.TryAdd(typeof(TInterface), (Scope.Singleton, CreateFunction<TImplementation>()));

        public void AlwaysUnique<TInterface, TImplementation>() =>
            _registeredTypes.TryAdd(typeof(TInterface), (Scope.AlwaysUnique, CreateFunction<TImplementation>()));

        public T Get<T>() =>
            CreateInstance<T>(new CreationContext(_registeredTypes, _createdSingletons));
    }
}
