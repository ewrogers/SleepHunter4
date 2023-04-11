using System;
using System.Collections.Generic;
using System.Linq;

namespace SleepHunter.Services
{
    public class ServiceCollection : IServiceProvider, IDisposable
    {
        private bool isDisposed;

        ~ServiceCollection() => Dispose(false);

        private readonly Dictionary<Type, ServiceMapping> typeMappings = new Dictionary<Type, ServiceMapping>();
        private readonly Dictionary<Type, object> singletons = new Dictionary<Type, object>();

        public void AddSingleton<T>() where T : class
        {
            CheckIfDisposed();
            AddServiceMapping(typeof(T), typeof(T), ServiceLifetime.Singleton);
        }

        public void AddSingleton<TInterface, T>() where T : class, TInterface
        {
            CheckIfDisposed();
            AddServiceMapping(typeof(TInterface), typeof(T), ServiceLifetime.Singleton);
        }

        public void AddTransient<T>() where T : class
        {
            CheckIfDisposed();
            AddServiceMapping(typeof(T), typeof(T), ServiceLifetime.Transient);
        }

        public void AddTransient<TInterface, T>() where T : class, TInterface
        {
            CheckIfDisposed();
            AddServiceMapping(typeof(TInterface), typeof(T), ServiceLifetime.Transient);
        }

        public bool IsRegistered<T>()
        {
            CheckIfDisposed();
            return typeMappings.ContainsKey(typeof(T));
        }

        public T GetService<T>()
        {
            CheckIfDisposed();
            return (T)GetServiceInternal(typeof(T));
        }

        private object GetServiceInternal(Type type)
        {
            if (!typeMappings.TryGetValue(type, out var mapping))
                throw new InvalidOperationException($"{type.Name} is not registered");

            var desiredType = mapping.Type;

            // Check if there is already a created singleton instance
            if (mapping.Lifetime == ServiceLifetime.Singleton && singletons.TryGetValue(desiredType, out var singletonInstance))
                return singletonInstance;

            var instance = InstantiateType(desiredType);

            // Store the singleton for later
            if (mapping.Lifetime == ServiceLifetime.Singleton)
                singletons.Add(desiredType, instance);

            return instance;
        }

        void AddServiceMapping(Type type, Type actualType, ServiceLifetime lifetime)
        {
            if (typeMappings.ContainsKey(type))
                throw new InvalidOperationException($"{type.Name} is already registered");

            typeMappings.Add(type, new ServiceMapping(actualType, lifetime));
        }

        object InstantiateType(Type type)
        {
            // Get all of the available constructors that have args which are already registered
            // Sort by descending so that the "largest" constructor is chosen first
            var availableConstructors = from ctor in type.GetConstructors()
                                        where ctor.GetParameters()
                                                         .All(arg => typeMappings.ContainsKey(arg.ParameterType))
                                        orderby ctor.GetParameters().Length descending
                                        select ctor;

            // Throw if no suitable constructor was found
            var constructor = availableConstructors.FirstOrDefault();
            if (constructor == null)
                throw new InvalidOperationException($"{type.Name} cannot be instantiated, no valid constructor found");

            // Dynamically build a list of arguments for the constructor
            var arguments = constructor.GetParameters().Select(arg => GetServiceInternal(arg.ParameterType)).ToArray();
            return Activator.CreateInstance(type, arguments);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            if (isDisposing)
            {
                foreach (var singletonObj in singletons.Values)
                {
                    if (singletonObj is IDisposable disposable)
                        disposable.Dispose();
                }

                singletons.Clear();
                typeMappings.Clear();
            }

            isDisposed = true;
        }

        private void CheckIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
        }
    }
}
