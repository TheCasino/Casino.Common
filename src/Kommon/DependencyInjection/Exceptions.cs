using System;

namespace Kommon.DependencyInjection {
	/// <inheritdoc />
	/// <summary>
	/// Thrown when a type passed to RunInitialisersAsync doesn't inherit from <seealso cref="BaseService{T}"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public sealed class InvalidServiceException<T> : Exception {
		/// <inheritdoc />
		/// <summary>
		/// </summary>
		/// <param name="type">The name of the type that doesn't inherit <see cref="BaseService{T}"/>.</param>
		public InvalidServiceException(string type) : base($"{type} does not inherit {nameof(BaseService<T>)}") { }
	}
}