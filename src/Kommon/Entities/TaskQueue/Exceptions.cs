using System;

namespace Casino.Common
{
    public sealed class TaskCompletedException : Exception
    {
        public TaskCompletedException() : base("Task has already been completed")
        {
        }
    }

    public sealed class TaskCancelledException : Exception
    {
        public TaskCancelledException() : base("Task has already been cancelled")
        {
        }
    }
}
