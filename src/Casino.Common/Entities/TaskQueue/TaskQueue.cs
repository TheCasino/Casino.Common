using System;
using System.Threading;
using System.Threading.Tasks;

namespace Casino.Common
{
    /// <summary>
    /// A simple task scheduler.
    /// </summary>
    public sealed partial class TaskQueue : IDisposable
    {
        private readonly PrioritisedCollection<IScheduledTask> _collection;
        private CancellationTokenSource _cts;

        private readonly object _queueLock;

        private IScheduledTask _currentTask;

        /// <summary>
        /// Creates a new TaskQueue.
        /// </summary>
        /// <param name="capacity">The capicity for the internal collection.</param>
        public TaskQueue(int? capacity)
        {
            _collection = new PrioritisedCollection<IScheduledTask>(
                tuple => tuple.Item1.ExecutionTime > tuple.Item2.ExecutionTime, capacity);

            _cts = new CancellationTokenSource();

            _queueLock = new object();
            _ = HandleCallbacksAsync();
        }

        /// <summary>
        /// Event that fires whenever there is an exception from a scheduled task.
        /// </summary>
        public event Func<Exception, Task> OnError;

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
                        wait = !_collection.MoveNext(out _currentTask);

                    if (wait)
                        await Task.Delay(-1, _cts.Token);

                    var time = _currentTask.ExecutionTime - DateTimeOffset.UtcNow;

                    if (time > TimeSpan.Zero)
                        await Task.Delay(time, _cts.Token);

                    if (_currentTask.IsCancelled)
                        continue;

                    await _currentTask.ExecuteAsync();
                    _currentTask.Completed();
                }
                catch (TaskCanceledException)
                {
                    lock (_queueLock)
                    {
                        if (_currentTask != null && !_currentTask.IsCancelled)
                            _collection.Add(_currentTask);

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
    }
}
