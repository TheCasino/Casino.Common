using Casino.Common.Linq;
using Qmmands;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Casino.Common.Qmmands
{
    public static class Extensions
    {
        private static IDictionary<Type, object> _parsers;

        /// <summary>
        /// Crawls the specified assembly for classes that inherit from <see cref="TypeParser{T}"/> and adds them to the <see cref="CommandService"/>.
        /// </summary>
        /// <param name="commands">Your command service.</param>
        /// <param name="assembly">The assembly you want to crawl.</param>
        public static CommandService AddTypeParsers(this CommandService commands, Assembly assembly)
        {
            var parsers = FindTypeParsers(commands, assembly);

            var internalAddParser = commands.GetType().GetMethod("AddTypeParserInternal",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (internalAddParser is null)
                throw new QuahuRenamedException("AddParserInternal");

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
            var typeParserInterface = commands.GetType().Assembly.GetTypes()
                .FirstOrDefault(x => x.Name == "ITypeParser")?.GetTypeInfo();

            if (typeParserInterface is null)
                throw new QuahuRenamedException("ITypeParser");

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
                type = commands.GetType();

                var field = type.GetField("_primitiveTypeParsers",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                if (field is null)
                    throw new QuahuRenamedException("_primitiveTypeParsers");

                var gen = (IDictionary) field.GetValue(commands);

                _parsers = gen.ToDictionary<Type, object>();
            }

            if (!_parsers.TryGetValue(typeof(T), out var parser))
            {
                return null;
            }

            type = parser.GetType();

            var method = type.GetMethod("TryParse");

            if (method is null)
                throw new QuahuRenamedException("TryParse");

            return new PrimitiveTypeParser<T>(method, parser);
        }
    }
}
