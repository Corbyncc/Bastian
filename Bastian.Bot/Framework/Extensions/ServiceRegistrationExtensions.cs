using System.Linq;
using System.Reflection;
using Bastian.Framework.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Bastian.Framework.Extensions;
public static class ServiceRegistrationExtensions
{
    public static IServiceCollection RegisterServiceImplementations(this IServiceCollection services, Assembly assembly)
    {
        var serviceTypes = assembly
            .GetTypes()
            .Where(t => t.IsInterface && t.GetCustomAttribute<ServiceAttribute>() != null);

        foreach (var serviceType in serviceTypes)
        {
            var implementationType = System.Array.Find(assembly.GetTypes(), t =>
                t.IsClass && !t.IsAbstract && serviceType.IsAssignableFrom(t) &&
                t.GetCustomAttribute<ServiceImplementationAttribute>() != null);

            if (implementationType != null)
            {
                var attribute = implementationType.GetCustomAttribute<ServiceImplementationAttribute>();
                var lifetime = attribute!.Lifetime;

                var serviceDescriptor = new ServiceDescriptor(serviceType, implementationType, lifetime);
                services.Add(serviceDescriptor);
            }
        }

        return services;
    }
}