using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Kommon.DependencyInjection {
	/// <summary>
	/// A class that serves as the base for services to be added to a <see cref="IServiceCollection"/>.
	/// </summary>
	/// <typeparam name="T">The type to use for the object passed to InitialiseAsync.</typeparam>
	public abstract class BaseService<T> {
		protected BaseService(IServiceProvider services) {
			services.Inject(this);
		}

		/// <summary>
		/// Method that initialises the service.
		/// </summary>
		/// <param name="services">Your <see cref="IServiceProvider"/>.</param>
		/// <param name="obj">Your object that you passed to RunInitialisersAsync.</param>
		/// <returns>An awaitable Task.</returns>
		public virtual Task InitialiseAsync(IServiceProvider services, T obj) {
			return Task.CompletedTask;
		}
	}
}