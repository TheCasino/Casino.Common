﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Kommon.DependencyInjection {
	public static class Extensions {
		public static IServiceCollection AddServices(this IServiceCollection collection, IEnumerable<Type> types) {
			foreach (Type type in types) {
				collection.AddSingleton(type);
			}

			return collection;
		}

		public static IServiceCollection
			AddServices(this IServiceCollection collection, IDictionary<Type, Type> types) {
			foreach ((Type type, Type impl) in types) {
				collection.AddSingleton(type, impl);
			}

			return collection;
		}

		public static void Inject(this IServiceProvider services, object obj) {
			Type objType = obj.GetType();

			IEnumerable<MemberInfo> members = objType
				.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
				.Where(x => x.GetCustomAttributes(typeof(InjectAttribute), true).Length > 0);

			Type type;
			object value;

			void SetFieldValue(FieldInfo field) {
				type = field.FieldType;
				value = services.GetRequiredService(type);

				field.SetValue(obj, value);
			}

			foreach (MemberInfo member in members) {
				switch (member) {
					case FieldInfo fieldInfo:
						SetFieldValue(fieldInfo);

						break;

					case PropertyInfo propInfo:
						type = propInfo.GetType();

						MethodInfo setMethod = propInfo.GetSetMethod(true);

						if (setMethod is null) {
							FieldInfo field = objType.GetField($"<{propInfo.Name}>k__BackingField",
								BindingFlags.NonPublic | BindingFlags.Instance);

							SetFieldValue(field);
						} else {
							value = services.GetRequiredService(type);

							propInfo.SetValue(obj, value);
						}

						break;
				}
			}
		}

		public static async Task
			RunInitialisersAsync<T>(this IServiceProvider services, T obj, IEnumerable<Type> types) {
			foreach (Type type in types) {
				object service = services.GetService(type);

				if (!(service is BaseService<T> validService)) {
					throw new InvalidServiceException<T>(nameof(type));
				}

				await validService.InitialiseAsync(services, obj);
			}
		}
	}
}