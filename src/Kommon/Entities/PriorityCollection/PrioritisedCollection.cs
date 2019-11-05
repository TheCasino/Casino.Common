using System;
using System.Collections;
using System.Collections.Generic;

namespace Kommon.Common {
	public class PrioritisedCollection : PrioritisedCollection<object> {
		public PrioritisedCollection(Predicate<(object, object)> predicate, int? capacity = null) : base(predicate,
			capacity) { }
	}

	/// <summary>
	/// A collection that orders items in a given priority/
	/// </summary>
	/// <typeparam name="T">The type of the collection.</typeparam>
	public class PrioritisedCollection<T> : IEnumerable<T> {
		private readonly List<T> _collection;
		private readonly object _lock;

		private readonly Predicate<(T, T)> _predicate;

		private T _highestPriority;
		private int _highestIndex;

		/// <summary>
		/// Creates a new PrioritisedCollection.
		/// </summary>
		/// <param name="predicate">Determine whether the current highest priority takes prioritry over the new item being added.</param>
		/// <param name="capacity">The capacity to use for the internal collection.</param>
		public PrioritisedCollection(Predicate<(T, T)> predicate, int? capacity = null) {
			if (capacity != null) {
				this._collection = new List<T>(capacity.Value);
			} else {
				this._collection = new List<T>();
			}

			this._lock = new object();

			this._predicate = predicate;
		}

		/// <summary>
		/// Adds a new item to the collection.
		/// </summary>
		/// <param name="obj">The item to add.</param>
		public void Add(T obj) {
			lock (this._lock) {
				if (this._collection.Count == 0) {
					this._highestIndex = 0;
					this._highestPriority = obj;
				}

				this._collection.Add(obj);

				if (this._collection.Count > 1 && this._predicate((this._highestPriority, obj))) {
					this._highestPriority = obj;
					this._highestIndex = this._collection.Count - 1;
				}
			}
		}

		/// <summary>
		/// Removes the current highest priority from the collection.
		/// </summary>
		/// <param name="obj">The current highest priority.</param>
		/// <returns>False if the collection is empty, true otherwise.</returns>
		public bool MoveNext(out T obj) {
			lock (this._lock) {
				obj = this._highestPriority;

				if (this._collection.Count == 0) {
					return false;
				}

				this._collection.RemoveAt(this._highestIndex);

				this._highestPriority = this._collection.Count == 0 ? default : this._collection[0];

				this._highestIndex = 0;

				for (var i = 1; i < this._collection.Count; i++) {
					T item = this._collection[i];

					if (this._predicate((this._highestPriority, item))) {
						this._highestPriority = item;
						this._highestIndex = i;
					}
				}

				return true;
			}
		}

		/// <summary>
		/// Clears the collection.
		/// </summary>
		public void Clear() {
			this._collection.Clear();
		}

		/// <summary>
		/// How many items are in the collection.
		/// </summary>
		public int Count => this._collection.Count;

		IEnumerator<T> IEnumerable<T>.GetEnumerator() {
			return this._collection.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return this._collection.GetEnumerator();
		}
	}
}