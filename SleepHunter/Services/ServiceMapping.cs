using System;

namespace SleepHunter.Services
{
    public class ServiceMapping
    {
        public Type Type { get; }
        public ServiceLifetime Lifetime { get; }

        public ServiceMapping(Type type, ServiceLifetime lifetime)
        {
            Type = type;
            Lifetime = lifetime;
        }

        public override string ToString() => $"{Type.Name} ({Lifetime})";
    }
}
