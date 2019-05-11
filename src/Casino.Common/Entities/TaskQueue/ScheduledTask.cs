using System;
using System.Threading.Tasks;

namespace Casino.Common
{
    /// <summary>
    /// An object that represents a task that has been scheduled.
    /// </summary>
    /// <typeparam name="T">The type you want your object to be in your task callback.</typeparam>
    public class ScheduledTask<T> : IScheduledTask
    {
        /// <summary>
        /// The <see cref="TaskQueue"/> that created this object.
        /// </summary>
        public TaskQueue Queue { get; }

        /// <summary>
        /// Whether the task has been cancelled or not.
        /// </summary>
        public bool IsCancelled { get; private set; }

        /// <summary>
        /// Whether the task has been completed or not.
        /// </summary>
        public bool HasCompleted { get; private set; }

        /// <summary>
        /// The object that gets passed to your callback.
        /// </summary>
        public T State { get; }

        /// <summary>
        /// The time at when the task will execute.
        /// </summary>
        public DateTimeOffset ExecutionTime { get; private set; }

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
        public Exception Exception { get; private set; }

        internal TaskCompletionSource<bool> Tcs { get; }
        internal Func<T, Task> ToExecute { get; }

        private readonly object _timeLock;

        internal ScheduledTask(TaskQueue queue, T obj, DateTimeOffset when, Func<T, Task> task)
        {
            Queue = queue;

            State = obj;
            ExecutionTime = when;
            ToExecute = task;

            Tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _timeLock = new object();
        }

        /// <summary>
        /// Cancels this task.
        /// </summary>
        public void Cancel()
        {
            IsCancelled = true;
        }

        /// <summary>
        /// Change the time at which this task will now run.
        /// </summary>
        /// <param name="executeIn">How long to wait before this task is executed.</param>
        public void Change(TimeSpan executeIn)
        {
            if(executeIn < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(executeIn));

            Change(DateTimeOffset.UtcNow.Add(executeIn));
        }

        /// <summary>
        /// Change the time at which this task will now run.
        /// </summary>
        /// <param name="executeAt">When you want this task to execute at.</param>
        public void Change(DateTimeOffset executeAt)
        {
            if(executeAt - DateTimeOffset.UtcNow < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(executeAt));

            lock (_timeLock)
            {
                ExecutionTime = executeAt;

                Queue.Reschedule();
            }
        }

        /// <summary>
        /// Waits until this task has been completed.
        /// </summary>
        /// <returns>An awaitable <see cref="Task"/></returns>
        public async Task WaitForCompletionAsync()
            => await Tcs.Task;

        bool IScheduledTask.IsCancelled => IsCancelled;

        Exception IScheduledTask.Exception => Exception;

        DateTimeOffset IScheduledTask.ExecutionTime => ExecutionTime;

        object IScheduledTask.State => State;

        Func<Task> IScheduledTask.ToExecute => () => ToExecute(State);

        void IScheduledTask.Cancel()
        {
            IsCancelled = true;
        }

        void IScheduledTask.Completed()
        {
            Tcs.SetResult(true);
            HasCompleted = true;
        }

        void IScheduledTask.SetException(Exception ex)
        {
            Exception = ex;
        }
    }

    internal interface IScheduledTask
    {
        bool IsCancelled { get; }
        Exception Exception { get; }
        DateTimeOffset ExecutionTime { get; }
        Func<Task> ToExecute { get; }
        object State { get; }

        void Cancel();
        void Completed();
        void SetException(Exception ex);
    }
}
