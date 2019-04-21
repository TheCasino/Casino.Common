using System;

namespace Casino.Common.Qmmands
{
    public class QuahuRenamedException : Exception
    {
        public QuahuRenamedException(string type) : base($"Quahu renamed {type}")
        {
        }
    }
}
