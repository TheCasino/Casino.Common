using Qmmands;
using System.Reflection;

namespace Casino.Qmmands
{
    /// <summary>
    /// A wrapper class for the internal primitive type parsers.
    /// </summary>
    /// <typeparam name="T">The type that the parser parses.</typeparam>
    public sealed class PrimitiveTypeParser<T>
    {
        private readonly MethodInfo _parseMethod;
        private readonly object _parser;

        internal PrimitiveTypeParser(MethodInfo parseMethod, object parser)
        {
            _parseMethod = parseMethod;
            _parser = parser;
        }

        /// <summary>
        /// Attempts to parse the given input
        /// </summary>
        /// <param name="parameter">The parameter that the parsing applies to.</param>
        /// <param name="value">The value you want to parse.</param>
        /// <param name="output">The output of the parse.</param>
        /// <returns>A bool that represents whether the parsing failed or not.</returns>
        public bool TryParse(Parameter parameter, string value, out T output)
        {
            var @params = new object[] { parameter, value, null };

            var result = (bool)_parseMethod.Invoke(_parser, @params);
            output = (T) (@params[2] ?? default(T));

            return result;
        }
    }
}
