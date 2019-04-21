using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Casino.Common.DependencyInjection
{
    public static class Extensions
    {
        public static IServiceCollection AddServices(this IServiceCollection collection, IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                collection.AddSingleton(type);
            }

            return collection;
        }

        public static void Inject(this IServiceProvider services, object obj)
        {
            var members = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => x.GetCustomAttributes(typeof(InjectAttribute), true).Length > 0)
                .ToArray();

            foreach (var member in members)
            {
                switch (member)
                {
                    case FieldInfo fieldInfo:
                        var type = fieldInfo.FieldType;

                        var value = services.GetService(type);

                        if (value is null)
                            continue;

                        fieldInfo.SetValue(obj, value);
                        break;
                }
            }
        }

        public static async Task RunInitialisersAsync<T>(this IServiceProvider services, T obj, IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                var service = services.GetService(type);

                if (!(service is BaseService<T> validService))
                    throw new InvalidServiceException<T>(nameof(type));

                await validService.InitialiseAsync(services, obj);
            }
        }
    }
}
