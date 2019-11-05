using System;

namespace Kommon.DependencyInjection {
	/// <inheritdoc />
	/// <summary>
	/// Members marked with this attribute will have their value automatically assigned when the class
	/// inherits <see cref="BaseService{T}"/> or when Inject is called.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class InjectAttribute : Attribute { }
}