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
        private readonly ConcurrentQueue<IScheduledTask> _taskQueue;
        private CancellationTokenSource _cts;

        private readonly object _queueLock;

        private IScheduledTask _currentTask;

        public TaskQueue()
        {
            _taskQueue = new ConcurrentQueue<IScheduledTask>();
            _cts = new CancellationTokenSource();

            _queueLock = new object();
            _ = HandleCallbacksAsync();
        }

        /// <summary>
        /// Event that fires whenever there is an exception from a scheduled task.
        /// </summary>
        public event Func<Exception, Task> OnError;

        /// <summary>
        /// Schedules a new task.
        /// </summary>
        /// <typeparam name="T">The type you want your object in the callback to be.</typeparam>
        /// <param name="obj">The object that you want to access in your callback.</param>
        /// <param name="executeIn">How long to wait before execution.</param>
        /// <param name="task">The task to be executed.</param>
        /// <returns>A <see cref="ScheduledTask{T}"/></returns>
        public ScheduledTask<T> ScheduleTask<T>(T obj, TimeSpan executeIn, Func<T, Task> task)
        {
            if (executeIn < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(executeIn));

            return ScheduleTask(obj, DateTimeOffset.UtcNow.Add(executeIn), task);
        }

        /// <summary>
        /// Schedules a new task.
        /// </summary>
        /// <typeparam name="T">The type you want your object in the callback to be.</typeparam>
        /// <param name="obj">The object that you want to access in your callback.</param>
        /// <param name="whenToExecute">The time at when this task needs to be ran.</param>
        /// <param name="task">The task to be executed.</param>
        /// <returns>A <see cref="ScheduledTask{T}"/></returns>
        public ScheduledTask<T> ScheduleTask<T>(T obj, DateTimeOffset whenToExecute, Func<T, Task> task)
        {
            if (whenToExecute - DateTimeOffset.UtcNow < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(whenToExecute));

            if (task is null)
                throw new ArgumentNullException(nameof(task));

            lock (_queueLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(TaskQueue));

                var toAdd = new ScheduledTask<T>(this, obj, whenToExecute, task);

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

                _currentTask.Cancel();

                while (_taskQueue.TryDequeue(out var task))
                {
                    task.Cancel();
                }

                _cts.Cancel(true);
            }
        }

        internal void Reschedule()
        {
            lock (_queueLock)
            {
                if(_disposed)
                    throw new ObjectDisposedException(nameof(TaskQueue));

                _cts.Cancel(true);
            }
        }

        private async Task HandleCallbacksAsync()
        {
            while (true)
            {
                if (_disposed)
                    break;

                try
                {
                    bool wait;

                    lock (_queueLock)
                        wait = !_taskQueue.TryDequeue(out _currentTask);

                    if (wait)
                        await Task.Delay(-1, _cts.Token);

                    var time = _currentTask.ExecutionTime - DateTimeOffset.UtcNow;

                    if (time > TimeSpan.Zero)
                    {
                        await Task.Delay(time, _cts.Token);
                    }

                    if (_currentTask.IsCancelled)
                        continue;

                    await _currentTask.ToExecute();
                    _currentTask.Completed();
                }
                catch (TaskCanceledException)
                {
                    lock (_queueLock)
                    {
                        if (_currentTask != null && !_currentTask.IsCancelled)
                            _taskQueue.Enqueue(_currentTask);

                        if (!_taskQueue.IsEmpty)
                        {
                            var copy = _taskQueue.ToArray().Where(x => !x.IsCancelled).OrderBy(x => x.ExecutionTime);

                            //Didn't do ClearQueue() since nested lock
                            while (_taskQueue.TryDequeue(out _))
                            {
                            }

                            foreach (var item in copy)
                                _taskQueue.Enqueue(item);
                        }

                        _cts.Dispose();
                        _cts = new CancellationTokenSource();
                    }
                }
                catch (Exception e)
                {
                    _currentTask?.SetException(e);

                    if (OnError != null)
                        await OnError(e);
                }
            }
        }

        private bool _disposed = false;

        private void Dispose(bool disposing)
        {
            lock (_queueLock)
            {
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
        }

        /// <inheritdoc />
        /// <summary>
        /// Disposes of the <see cref="TaskQueue" /> and frees up any managed resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
    }
}
