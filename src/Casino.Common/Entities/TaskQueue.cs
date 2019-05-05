using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Casino.Common
{
    /// <inheritdoc />
    /// <summary>
    /// A simple task scheduler.
    /// </summary>
    public sealed class TaskQueue : IDisposable
    {
        private readonly ConcurrentQueue<ScheduledTask> _taskQueue;
        private CancellationTokenSource _cts;

        private readonly object _queueLock;

        public TaskQueue()
        {
            _taskQueue = new ConcurrentQueue<ScheduledTask>();
            _cts = new CancellationTokenSource();

            _queueLock = new object();
            _ = HandleCallbacksAsync();
        }

        /// <summary>
        /// Event that fires whenever there is an exception from a scheduled task.
        /// </summary>
        public event Func<Exception, Task> OnError;

        /// <summary>
        /// Schedule a new task.
        /// </summary>
        /// <param name="obj">An object this task depends upon.</param>
        /// <param name="whenToRun">The time at when this task needs to be ran.</param>
        /// <param name="task">The task to be executed.</param>
        /// <returns>A <see cref="ScheduledTask"/>.</returns>
        public ScheduledTask ScheduleTask(object obj, DateTimeOffset whenToRun, Func<object, Task> task)
        {
            if (whenToRun - DateTimeOffset.UtcNow < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(whenToRun));

            if (task is null)
                throw new ArgumentNullException(nameof(task));

            lock (_queueLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(TaskQueue));

                var toAdd = new ScheduledTask(obj, whenToRun, task);

                _taskQueue.Enqueue(toAdd);
                _cts.Cancel(true);

                return toAdd;
            }
        }

        /// <summary>
        /// Clears and cancels all the currently scheduled tasks from the queue.
        /// </summary>
        public void ClearQueue()
        {
            lock (_queueLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(TaskQueue));

                while (_taskQueue.TryDequeue(out var task))
                {
                    task.Cancel();
                }

                _cts.Cancel(true);
            }
        }

        private async Task HandleCallbacksAsync()
        {
            while (true)
            {
                if (_disposed)
                    break;

                ScheduledTask task = null;

                try
                {
                    bool wait;

                    lock (_queueLock)
                        wait = !_taskQueue.TryDequeue(out task);

                    if (wait)
                        await Task.Delay(-1, _cts.Token);

                    var time = task.WhenToRun - DateTimeOffset.UtcNow;

                    if (time > TimeSpan.Zero)
                    {
                        await Task.Delay(time, _cts.Token);
                    }

                    if (task.IsCancelled)
                        continue;

                    await task.Task(task.Object);
                    task.Tcs.SetResult(true);
                }
                catch (TaskCanceledException)
                {
                    lock (_queueLock)
                    {
                        if (task != null)
                            _taskQueue.Enqueue(task);

                        var copy = _taskQueue.ToArray().Where(x => !x.IsCancelled)
                            .OrderBy(x => x.WhenToRun);

                        //Didn't do ClearQueue() since nested lock
                        while (_taskQueue.TryDequeue(out _))
                        {
                        }

                        foreach(var item in copy)
                            _taskQueue.Enqueue(item);

                        _cts.Dispose();
                        _cts = new CancellationTokenSource();
                    }
                }
                catch (Exception e)
                {
                    if (!(OnError is null))
                        await OnError(e);
                }
            }
        }

        private bool _disposed = false;

        private void Dispose(bool disposing)
        {
            lock (_queueLock)
                if (!_disposed)
                {
                    if (disposing)
                    {
                        _cts.Cancel(true);
                        _cts.Dispose();
                    }

                    _disposed = true;
                }
        }

        /// <inheritdoc />
        /// <summary>
        /// Disposes of the <see cref="T:Casino.Common.TaskQueue" /> and frees up any managed resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
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
        /// The object that will be passed to the tasks callback.
        /// </summary>
        public object Object { get; }

        /// <summary>
        /// When the task needs to be ran.
        /// </summary>
        public DateTimeOffset WhenToRun { get; }

        internal TaskCompletionSource<bool> Tcs { get; }
        internal Func<object, Task> Task { get; }

        internal ScheduledTask(object obj, DateTimeOffset when, Func<object, Task> task)
        {
            Object = obj;
            WhenToRun = when;
            Task = task;

            Tcs = new TaskCompletionSource<bool>();
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
        /// <returns>An awaitable <see cref="System.Threading.Tasks.Task"/></returns>
        public Task WaitForCompletionAsync()
            => Tcs.Task;
    }
}
