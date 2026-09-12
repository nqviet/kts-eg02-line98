using System;
using System.Collections.Generic;

namespace Line98.App
{
    /// <summary>
    /// Simple, high-performance, strongly typed manual service locator.
    /// Hand-rolled to avoid heavy DI container overhead (~300 LOC principle).
    /// </summary>
    public sealed class ServiceRegistry
    {
        private static ServiceRegistry s_Instance;
        public static ServiceRegistry Instance => s_Instance ?? (s_Instance = new ServiceRegistry());

        private readonly Dictionary<Type, object> m_Services = new Dictionary<Type, object>();

        public static void SetInstance(ServiceRegistry instance)
        {
            s_Instance = instance;
        }

        public void Register<T>(T service) where T : class
        {
            m_Services[typeof(T)] = service;
        }

        public T Resolve<T>() where T : class
        {
            if (m_Services.TryGetValue(typeof(T), out object service))
            {
                return (T)service;
            }
            return null;
        }

        public bool TryResolve<T>(out T service) where T : class
        {
            if (m_Services.TryGetValue(typeof(T), out object obj))
            {
                service = (T)obj;
                return true;
            }
            service = null;
            return false;
        }

        public void Clear()
        {
            m_Services.Clear();
        }
    }
}
