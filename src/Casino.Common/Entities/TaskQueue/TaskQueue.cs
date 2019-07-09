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

        private static readonly TimeSpan _maxTime = TimeSpan.FromMilliseconds(int.MaxValue);

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
            while (!_disposed)
            {
                try
                {
                    bool wait;

                    lock (_queueLock)
                        wait = !_collection.MoveNext(out _currentTask);

                    if (wait)
                        await Task.Delay(-1, _cts.Token).ConfigureAwait(false);

                    var time = _currentTask.ExecutionTime - DateTimeOffset.UtcNow;

                    while(time > _maxTime)
                    {
                        await Task.Delay(_maxTime, _cts.Token).ConfigureAwait(false);
                        time = _currentTask.ExecutionTime - DateTimeOffset.UtcNow;
                    }

                    if (time > TimeSpan.Zero)
                        await Task.Delay(time, _cts.Token).ConfigureAwait(false);

                    if (_currentTask.IsCancelled)
                        continue;

                    await _currentTask.ExecuteAsync().ConfigureAwait(false);
                    _currentTask.Completed();
                }
                catch (TaskCanceledException)
                {
                    lock (_queueLock)
                    {
                        if (_currentTask?.IsCancelled == false)
                            _collection.Add(_currentTask);

                        _cts.Dispose();
                        _cts = new CancellationTokenSource();
                    }
                }
                catch (Exception e)
                {
                    _currentTask?.SetException(e);

                    if (OnError != null)
                        await OnError(e).ConfigureAwait(false);
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
