using System;
using System.Threading.Tasks;

namespace Kommon.Common {
	public sealed partial class TaskQueue {
		/// <summary>
		/// Schedules a new task.
		/// </summary>
		/// <typeparam name="T">The type you want your object in the callback to be.</typeparam>
		/// <param name="state">The object that you want to access in your callback.</param>
		/// <param name="executeIn">How long to wait before execution.</param>
		/// <param name="callback">The task to be executed.</param>
		/// <returns>A <see cref="ScheduledTask{T}"/></returns>
		public ScheduledTask<T> ScheduleTask<T>(T state, TimeSpan executeIn, Func<T, Task> callback) {
			ArgChecks(executeIn);
			return ScheduleTask(state, DateTimeOffset.UtcNow.Add(executeIn), callback);
		}

		/// <summary>
		/// Schedules a new task.
		/// </summary>
		/// <typeparam name="T">The type you want your object in the callback to be.</typeparam>
		/// <param name="state">The object that you want to access in your callback.</param>
		/// <param name="whenToExecute">The time at when this task needs to be ran.</param>
		/// <param name="callback">The task to be executed.</param>
		/// <returns>A <see cref="ScheduledTask{T}"/></returns>
		public ScheduledTask<T> ScheduleTask<T>(T state, DateTimeOffset whenToExecute, Func<T, Task> callback) {
			ArgChecks(whenToExecute, callback);

			lock (this._queueLock) {
				if (this._disposed) {
					throw new ObjectDisposedException(nameof(TaskQueue));
				}

				var toAdd = new ScheduledTask<T>(this, state, whenToExecute, callback);

				this._collection.Add(toAdd);
				this._cts.Cancel(true);

				return toAdd;
			}
		}

		/// <summary>
		/// Schedules a new task.
		/// </summary>
		/// <param name="state">The object that you want to access in your callback.</param>
		/// <param name="executeIn">How long to wait before execution.</param>
		/// <param name="callback">The task to be executed.</param>
		/// <returns>A <see cref="ScheduledTask"/></returns>
		public ScheduledTask ScheduleTask(object state, TimeSpan executeIn, Func<object, Task> callback) {
			ArgChecks(executeIn);
			return ScheduleTask(state, DateTimeOffset.UtcNow.Add(executeIn), callback);
		}

		/// <summary>
		/// Schedules a new task.
		/// </summary>
		/// <param name="state">The object that you want to access in your callback.</param>
		/// <param name="whenToExecute">The time at when this task needs to be ran.</param>
		/// <param name="callback">The task to be executed.</param>
		/// <returns>A <see cref="ScheduledTask"/></returns>
		public ScheduledTask ScheduleTask(object state, DateTimeOffset whenToExecute, Func<object, Task> callback) {
			ArgChecks(whenToExecute, callback);

			lock (this._queueLock) {
				if (this._disposed) {
					throw new ObjectDisposedException(nameof(TaskQueue));
				}

				var toAdd = new ScheduledTask(this, state, whenToExecute, callback);

				this._collection.Add(toAdd);
				this._cts.Cancel(true);

				return toAdd;
			}
		}

		/// <summary>
		/// Schedules a new task.
		/// </summary>
		/// <param name="executeIn">How long to wait before execution.</param>
		/// <param name="callback">The task to be executed.</param>
		/// <returns>A <see cref="ScheduledTask"/></returns>
		public ScheduledTask ScheduleTask(TimeSpan executeIn, Func<Task> callback) {
			ArgChecks(executeIn);
			return ScheduleTask(DateTimeOffset.UtcNow.Add(executeIn), callback);
		}

		/// <summary>
		/// Schedules a new task.
		/// </summary>
		/// <param name="whenToExecute">The time at when this task needs to be ran.</param>
		/// <param name="callback">The task to be executed.</param>
		/// <returns>A <see cref="ScheduledTask"/></returns>
		public ScheduledTask ScheduleTask(DateTimeOffset whenToExecute, Func<Task> callback) {
			if (whenToExecute - DateTimeOffset.UtcNow < TimeSpan.Zero) {
				throw new ArgumentOutOfRangeException(nameof(whenToExecute));
			}

			if (callback is null) {
				throw new ArgumentNullException(nameof(callback));
			}

			lock (this._queueLock) {
				if (this._disposed) {
					throw new ObjectDisposedException(nameof(TaskQueue));
				}

				var toAdd = new ScheduledTask(this, null, whenToExecute, _ => callback());

				this._collection.Add(toAdd);
				this._cts.Cancel(true);

				return toAdd;
			}
		}

		private void ArgChecks<T>(DateTimeOffset whenToExecute, Func<T, Task> callback) {
			if (whenToExecute - DateTimeOffset.UtcNow < TimeSpan.Zero) {
				throw new ArgumentOutOfRangeException(nameof(whenToExecute));
			}

			if (callback is null) {
				throw new ArgumentNullException(nameof(callback));
			}
		}

		private void ArgChecks(TimeSpan executeIn) {
			if (executeIn < TimeSpan.Zero) {
				throw new ArgumentOutOfRangeException(nameof(executeIn));
			}
		}

		/// <summary>
		/// Clears and cancels all the currently scheduled tasks from the queue.
		/// </summary>
		public void ClearQueue() {
			lock (this._queueLock) {
				if (this._disposed) {
					throw new ObjectDisposedException(nameof(TaskQueue));
				}

				this._currentTask.Cancel();
				this._collection.Clear();

				this._cts.Cancel(true);
			}
		}

		/// <summary>
		/// Disposes of the <see cref="TaskQueue"/> and frees up any managed resources.
		/// </summary>
		public void Dispose() {
			Dispose(true);
		}

		internal void Reschedule() {
			lock (this._queueLock) {
				if (this._disposed) {
					throw new ObjectDisposedException(nameof(TaskQueue));
				}

				this._cts.Cancel(true);
			}
		}
	}
}