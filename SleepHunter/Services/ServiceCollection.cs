using System;
using System.Collections.Generic;

namespace SleepHunter.Services
{
    internal class ServiceCollection
    {
        private readonly Dictionary<Type, ServiceMapping> typeMappings = new Dictionary<Type, ServiceMapping>();

        public void AddSingleton<T>() where T : class
            => AddServiceMapping(typeof(T), typeof(T), ServiceLifetime.Singleton);

        public void AddSingleton<TInterface, T>() where T : class, TInterface
            => AddServiceMapping(typeof(TInterface), typeof(T), ServiceLifetime.Singleton);

        public void AddTransient<T>() where T : class
            => AddServiceMapping(typeof(T), typeof(T), ServiceLifetime.Transient);

        public void AddTransient<TInterface, T>() where T : class, TInterface
            => AddServiceMapping(typeof(TInterface), typeof(T), ServiceLifetime.Transient);

        void AddServiceMapping(Type type, Type actualType, ServiceLifetime lifetime)
        {
            if (typeMappings.ContainsKey(type))
                throw new InvalidOperationException($"{type.Name} is already registered");

            typeMappings.Add(type, new ServiceMapping(actualType, lifetime));
        }

        public IServiceProvider BuildServiceProvider() => new ServiceProvider(typeMappings);
    }
}
