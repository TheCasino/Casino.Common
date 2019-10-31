using System;

namespace Kommon.Qmmands
{
    public class QuahuRenamedException : Exception
    {
        public QuahuRenamedException(string type) : base($"Quahu renamed {type}")
        {
        }
    }

    public class MissingParserException : Exception
    {
        public MissingParserException(Type type) : base($"Custom parser was not passed when fetching the primite {type}")
        {
        }
    }
}
