using Casino.Linq;
using Qmmands;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Module = Qmmands.Module;

namespace Casino.Qmmands
{
    public static class Extensions
    {
        private static IDictionary<Type, object> _parsers;
        private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// Crawls the specified assembly for classes that inherit from <see cref="TypeParser{T}"/> and adds them to the <see cref="CommandService"/>.
        /// </summary>
        /// <param name="commands">Your command service.</param>
        /// <param name="assembly">The assembly you want to crawl.</param>
        public static CommandService AddTypeParsers(this CommandService commands, Assembly assembly)
        {
            const string addParserName = "AddTypeParserInternal";

            var parsers = FindTypeParsers(commands, assembly);

            var internalAddParser = commands.GetType().GetMethod(addParserName, Flags);

            if (internalAddParser is null)
                throw new QuahuRenamedException(addParserName);

            foreach (var parser in parsers)
            {
                var @override = parser.GetCustomAttribute<DontOverrideAttribute>() is null;

                var targetType = parser.BaseType?.GetGenericArguments().First();

                internalAddParser.Invoke(commands, new[] { targetType, Activator.CreateInstance(parser), @override });
            }

            return commands;
        }

        /// <summary>
        /// Crawls the specified assembly for all the classes that inherit from <see cref="TypeParser{T}"/>.
        /// </summary>
        /// <param name="commands">Your command service.</param>
        /// <param name="assembly">The assembly you want to crawl.</param>
        /// <returns>A collection of types that inherit from <see cref="TypeParser{T}"/>.</returns>
        public static IReadOnlyCollection<Type> FindTypeParsers(this CommandService commands, Assembly assembly)
        {
            const string parserInterface = "ITypeParser";

            var typeParserInterface = commands.GetType().Assembly.GetTypes()
                .FirstOrDefault(x => x.Name == parserInterface)?.GetTypeInfo();

            if (typeParserInterface is null)
                throw new QuahuRenamedException(parserInterface);

            var parsers = assembly.GetTypes().Where(x => typeParserInterface.IsAssignableFrom(x) && !x.IsAbstract);

            return parsers.ToArray();
        }

        /// <summary>
        /// Gets the primitive type parser for the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the parser you want to get.</typeparam>
        /// <param name="commands">Your <see cref="CommandService"/></param>
        /// <returns>The primitive parser corresponding to that type, null if none is found.</returns>
        public static PrimitiveTypeParser<T> GetPrimiteTypeParser<T>(this CommandService commands)
        {
            Type type;

            if (_parsers is null)
            {
                const string primitiveName = "_primitiveTypeParsers";

                type = commands.GetType();

                var field = type.GetField(primitiveName, Flags);

                if (field is null)
                    throw new QuahuRenamedException(primitiveName);

                var gen = (IDictionary) field.GetValue(commands);

                _parsers = gen.ToDictionary<Type, object>();
            }

            if (!_parsers.TryGetValue(typeof(T), out var parser))
            {
                return null;
            }

            type = parser.GetType();

            const string tryParse = "TryParse";

            var method = type.GetMethod(tryParse);

            if (method is null)
                throw new QuahuRenamedException(tryParse);

            return new PrimitiveTypeParser<T>(method, parser);
        }

        /// <summary>
        /// Modifies the command.
        /// </summary>
        /// <param name="command">The command you want to modify.</param>
        /// <param name="builder">The modifications you want to make.</param>
        /// <param name="mBuilder">Modifications you want applied to the <see cref="ModuleBuilder"/> that command belongs.</param>
        public static void Modify(this Command command, Action<CommandBuilder> builder, Action<ModuleBuilder> mBuilder = null)
        {
            var type = command.GetType();

            var commands = command.Service;

            const string singatureName = "SignatureIdentifier";

            var field = type.GetField(singatureName, Flags);

            if(field is null)
                throw new QuahuRenamedException(singatureName);

            var (hasRemainder, signature) = ((bool, string)) field.GetValue(command);

            var module = command.Module;

            commands.RemoveModule(module);
            commands.AddModule(module.Type, moduleBuilder =>
            {
                mBuilder?.Invoke(moduleBuilder);

                (bool HasRemainder, string Signature) BuildSignature(CommandBuilder commandBuilder)
                {
                    var sb = new StringBuilder();
                    var f = false;

                    for (var i = 0; i < commandBuilder.Parameters.Count; i++)
                    {
                        var parameter = commandBuilder.Parameters[i];

                        if (parameter.IsRemainder)
                            f = true;

                        sb.Append(parameter.Type).Append(';');
                    }

                    return (f, sb.ToString());
                }

                foreach (var commandBuilder in moduleBuilder.Commands)
                {
                    var (cHasRemainder, cSignature) = BuildSignature(commandBuilder);

                    if (cHasRemainder == hasRemainder && cSignature == signature)
                    {
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
        public static void Modify(this Module module, Action<ModuleBuilder> builder)
        {
            var commands = module.Service;

            commands.RemoveModule(module);
            commands.AddModule(module.Type, builder);
        }
    }
}