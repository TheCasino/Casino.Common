using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kommon.Common {
	/// <summary>
	/// A simple task scheduler.
	/// </summary>
	public sealed partial class TaskQueue : IDisposable {
		private readonly PrioritisedCollection<IScheduledTask> _collection;
		private CancellationTokenSource _cts;

		private readonly object _queueLock;

		private IScheduledTask _currentTask;

		private static readonly TimeSpan _maxTime = TimeSpan.FromMilliseconds(int.MaxValue);

		/// <summary>
		/// Creates a new TaskQueue.
		/// </summary>
		/// <param name="capacity">The capicity for the internal collection.</param>
		public TaskQueue(int? capacity) {
			this._collection = new PrioritisedCollection<IScheduledTask>(
				tuple => tuple.Item1.ExecutionTime > tuple.Item2.ExecutionTime, capacity);

			this._cts = new CancellationTokenSource();

			this._queueLock = new object();
			_ = HandleCallbacksAsync();
		}

		/// <summary>
		/// Event that fires whenever there is an exception from a scheduled task.
		/// </summary>
		public event Func<Exception, Task> OnError;

		private async Task HandleCallbacksAsync() {
			while (!this._disposed) {
				try {
					bool wait;

					lock (this._queueLock) {
						wait = !this._collection.MoveNext(out this._currentTask);
					}

					if (wait) {
						await Task.Delay(-1, this._cts.Token).ConfigureAwait(false);
					}

					TimeSpan time = this._currentTask.ExecutionTime - DateTimeOffset.UtcNow;

					while (time > _maxTime) {
						await Task.Delay(_maxTime, this._cts.Token).ConfigureAwait(false);
						time = this._currentTask.ExecutionTime - DateTimeOffset.UtcNow;
					}

					if (time > TimeSpan.Zero) {
						await Task.Delay(time, this._cts.Token).ConfigureAwait(false);
					}

					if (this._currentTask.IsCancelled) {
						continue;
					}

					await this._currentTask.ExecuteAsync().ConfigureAwait(false);
					this._currentTask.Completed();
				} catch (TaskCanceledException) {
					lock (this._queueLock) {
						if (this._currentTask?.IsCancelled == false) {
							this._collection.Add(this._currentTask);
						}

						this._cts.Dispose();
						this._cts = new CancellationTokenSource();
					}
				} catch (Exception e) {
					this._currentTask?.SetException(e);

					if (OnError != null) {
						await OnError(e).ConfigureAwait(false);
					}
				}
			}
		}

		private bool _disposed = false;

		private void Dispose(bool disposing) {
			lock (this._queueLock) {
				if (!this._disposed) {
					if (disposing) {
						this._cts.Cancel(true);
						this._cts.Dispose();
					}

					this._disposed = true;
				}
			}
		}
	}
}