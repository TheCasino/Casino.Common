using System;
using Qmmands;
using System.Reflection;
using System.Threading.Tasks;

namespace Casino.Common.Qmmands
{
    /// <inheritdoc />
    /// <summary>
    /// A wrapper class for the internal primite type parsers.
    /// </summary>
    /// <typeparam name="T">The type that the parser parses.</typeparam>
    public sealed class PrimiteTypeParser<T> : TypeParser<T>
    {
        private readonly MethodInfo _parseMethod;
        private readonly object _parser;
        private readonly Func<Parameter, string, CommandContext, Task<TypeParserResult<T>>> _func;

        internal PrimiteTypeParser(MethodInfo parseMethod, object parser,
            Func<Parameter, string, CommandContext, Task<TypeParserResult<T>>> func = null)
        {
            _parseMethod = parseMethod;
            _parser = parser;
            _func = func;
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
            output = (T)@params[2];

            return result;
        }

        /// <inheritdoc />
        public override Task<TypeParserResult<T>> ParseAsync(Parameter parameter, string value, CommandContext context,
            IServiceProvider provider)
        {
            if(_func is null)
                throw new MissingParserException(typeof(T));

            return _func(parameter, value, context);
        }
    }
}
