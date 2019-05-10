using System;
using System.Threading.Tasks;

namespace Casino.Common
{
    /// <inheritdoc />
    /// <summary>
    /// An object that represents a generic <see cref="ScheduledTask"/>.
    /// </summary>
    /// <typeparam name="T">The type you want your object to be in your task callback.</typeparam>
    public class ScheduledTask<T> : ScheduledTask
    {
        public new T Object { get; }

        internal ScheduledTask(T obj, DateTimeOffset when, Func<T, Task> task) : base(obj, when, ob => task((T)ob))
        {
            Object = obj;
        }
    }

    /// <summary>
    /// An object that represents a scheduled task.
    /// </summary>
    public class ScheduledTask
    {
        /// <summary>
        /// Whether the task has been cancelled or not.
        /// </summary>
        public bool IsCancelled { get; private set; }

        /// <summary>
        /// Whether the task has been completed or not.
        /// </summary>
        public bool HasCompleted { get; private set; }

        /// <summary>
        /// The object that will be passed to the tasks callback.
        /// </summary>
        public object Object { get; }

        /// <summary>
        /// The time at when the task will execute.
        /// </summary>
        public DateTimeOffset ExecutionTime { get; }

        /// <summary>
        /// Gets how long until this task executes.
        /// </summary>
        public TimeSpan ExecutesIn
        {
            get
            {
                var time = ExecutionTime - DateTimeOffset.UtcNow;

                return time > TimeSpan.Zero ? time : TimeSpan.FromSeconds(-1);
            }
        }

        /// <summary>
        /// Gets the exception (if thrown) from execution.
        /// </summary>
        public Exception Exception { get; internal set; }

        internal TaskCompletionSource<bool> Tcs { get; }
        internal Func<object, Task> ToExecute { get; }

        internal ScheduledTask(object obj, DateTimeOffset when, Func<object, Task> task)
        {
            Object = obj;
            ExecutionTime = when;
            ToExecute = task;

            Tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        /// <summary>
        /// Cancels this task.
        /// </summary>
        public void Cancel()
        {
            IsCancelled = true;
        }

        /// <summary>
        /// Waits until this task has been completed.
        /// </summary>
        /// <returns>An awaitable <see cref="Task"/></returns>
        public async Task WaitForCompletionAsync()
            => await Tcs.Task;

        internal void Completed()
        {
            Tcs.SetResult(true);
            HasCompleted = true;
        }
    }
}
