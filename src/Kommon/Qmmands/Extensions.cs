using Kommon.Linq;
using Qmmands;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using Module = Qmmands.Module;

namespace Kommon.Qmmands {
	public static class Extensions {
		private static IDictionary<Type, object> _parsers;
		private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

		/// <summary>
		/// Crawls the specified assembly for classes that inherit from <see cref="TypeParser{T}"/> and adds them to the <see cref="CommandService"/>.
		/// </summary>
		/// <param name="commands">Your command service.</param>
		/// <param name="assembly">The assembly you want to crawl.</param>
		public static CommandService AddTypeParsers(this CommandService commands, Assembly assembly) {
			const string addParserName = "AddTypeParserInternal";

			IReadOnlyList<Type> parsers = FindTypeParsers(commands, assembly);

			MethodInfo internalAddParser = commands.GetType().GetMethod(addParserName, Flags);

			if (internalAddParser is null) {
				throw new QuahuRenamedException(addParserName);
			}

			foreach (Type parser in parsers) {
				bool @override = parser.GetCustomAttribute<DontOverrideAttribute>() is null;

				Type targetType = parser.BaseType?.GetGenericArguments().First();

				internalAddParser.Invoke(commands, new[] {
					targetType,
					Activator.CreateInstance(parser),
					@override
				});
			}

			return commands;
		}

		/// <summary>
		/// Crawls the specified assembly for all the classes that inherit from <see cref="TypeParser{T}"/>.
		/// </summary>
		/// <param name="commands">Your command service.</param>
		/// <param name="assembly">The assembly you want to crawl.</param>
		/// <returns>A collection of types that inherit from <see cref="TypeParser{T}"/>.</returns>
		public static IReadOnlyList<Type> FindTypeParsers(this CommandService commands, Assembly assembly) {
			const string parserInterface = "ITypeParser";

			TypeInfo typeParserInterface = commands.GetType().Assembly.GetTypes()
				.FirstOrDefault(x => x.Name == parserInterface)?.GetTypeInfo();

			if (typeParserInterface is null) {
				throw new QuahuRenamedException(parserInterface);
			}

			IEnumerable<Type> parsers =
				assembly.GetTypes().Where(x => typeParserInterface.IsAssignableFrom(x) && !x.IsAbstract);

			return parsers.ToImmutableArray();
		}

		/// <summary>
		/// Gets the primitive type parser for the specified type.
		/// </summary>
		/// <typeparam name="T">The type of the parser you want to get.</typeparam>
		/// <param name="commands">Your <see cref="CommandService"/></param>
		/// <returns>The primitive parser corresponding to that type, null if none is found.</returns>
		public static PrimitiveTypeParser<T> GetPrimiteTypeParser<T>(this CommandService commands) {
			Type type;

			if (_parsers is null) {
				const string primitiveName = "_primitiveTypeParsers";

				type = commands.GetType();

				FieldInfo field = type.GetField(primitiveName, Flags);

				if (field is null) {
					throw new QuahuRenamedException(primitiveName);
				}

				var gen = (IDictionary) field.GetValue(commands);

				_parsers = gen.ToDictionary<Type, object>();
			}

			if (!_parsers.TryGetValue(typeof(T), out object parser)) {
				return null;
			}

			type = parser.GetType();

			const string tryParse = "TryParse";

			MethodInfo method = type.GetMethod(tryParse);

			if (method is null) {
				throw new QuahuRenamedException(tryParse);
			}

			return new PrimitiveTypeParser<T>(method, parser);
		}

		/// <summary>
		/// Modifies the command.
		/// </summary>
		/// <param name="command">The command you want to modify.</param>
		/// <param name="builder">The modifications you want to make.</param>
		/// <param name="mBuilder">Modifications you want applied to the <see cref="ModuleBuilder"/> that command belongs.</param>
		public static void Modify(this Command command, Action<CommandBuilder> builder,
			Action<ModuleBuilder> mBuilder = null) {
			Type type = command.GetType();

			CommandService commands = command.Service;

			const string singatureName = "SignatureIdentifier";

			FieldInfo field = type.GetField(singatureName, Flags);

			if (field is null) {
				throw new QuahuRenamedException(singatureName);
			}

			(bool hasRemainder, string signature) = ((bool, string)) field.GetValue(command);

			Module module = command.Module;

			commands.RemoveModule(module);
			commands.AddModule(module.Type, moduleBuilder => {
				mBuilder?.Invoke(moduleBuilder);

				(bool HasRemainder, string Signature) BuildSignature(CommandBuilder commandBuilder) {
					var sb = new StringBuilder();
					var f = false;

					for (var i = 0; i < commandBuilder.Parameters.Count; i++) {
						ParameterBuilder parameter = commandBuilder.Parameters[i];

						if (parameter.IsRemainder) {
							f = true;
						}

						sb.Append(parameter.Type).Append(';');
					}

					return (f, sb.ToString());
				}

				foreach (CommandBuilder commandBuilder in moduleBuilder.Commands) {
					(bool cHasRemainder, string cSignature) = BuildSignature(commandBuilder);

					if (cHasRemainder == hasRemainder && cSignature == signature) {
						builder.Invoke(commandBuilder);
						break;
					}
				}
			});
		}

		/// <summary>
		/// Modifies the module.
		/// </summary>
		/// <param name="module">The module you want to modify.</param>
		/// <param name="builder">The modifications you want to make.</param>
		public static void Modify(this Module module, Action<ModuleBuilder> builder) {
			CommandService commands = module.Service;

			commands.RemoveModule(module);
			commands.AddModule(module.Type, builder);
		}
	}
}