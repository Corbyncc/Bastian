using System;
using Microsoft.Extensions.DependencyInjection;

namespace Bastian.Framework.Attributes;
[AttributeUsage(AttributeTargets.Class)]
public class ServiceImplementationAttribute : Attribute
{
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;
}