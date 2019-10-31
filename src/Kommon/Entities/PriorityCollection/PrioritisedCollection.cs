using System;
using System.Collections;
using System.Collections.Generic;

namespace Kommon.Common
{
    public class PrioritisedCollection : PrioritisedCollection<object>
    {
        public PrioritisedCollection(Predicate<(object, object)> predicate, int? capacity = null) : base(predicate, capacity)
        {
        }
    }

    /// <summary>
    /// A collection that orders items in a given priority/
    /// </summary>
    /// <typeparam name="T">The type of the collection.</typeparam>
    public class PrioritisedCollection<T> : IEnumerable<T>
    {
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
        public PrioritisedCollection(Predicate<(T, T)> predicate, int? capacity = null)
        {
            if (capacity != null)
                _collection = new List<T>(capacity.Value);
            else
                _collection = new List<T>();

            _lock = new object();

            _predicate = predicate;
        }

        /// <summary>
        /// Adds a new item to the collection.
        /// </summary>
        /// <param name="obj">The item to add.</param>
        public void Add(T obj)
        {
            lock (_lock)
            {
                if (_collection.Count == 0)
                {
                    _highestIndex = 0;
                    _highestPriority = obj;
                }

                _collection.Add(obj);

                if (_collection.Count > 1 && _predicate((_highestPriority, obj)))
                {
                    _highestPriority = obj;
                    _highestIndex = _collection.Count - 1;
                }
            }
        }

        /// <summary>
        /// Removes the current highest priority from the collection.
        /// </summary>
        /// <param name="obj">The current highest priority.</param>
        /// <returns>False if the collection is empty, true otherwise.</returns>
        public bool MoveNext(out T obj)
        {
            lock (_lock)
            {
                obj = _highestPriority;

                if (_collection.Count == 0)
                    return false;

                _collection.RemoveAt(_highestIndex);

                _highestPriority = _collection.Count == 0 ? default : _collection[0];

                _highestIndex = 0;

                for (int i = 1; i < _collection.Count; i++)
                {
                    var item = _collection[i];

                    if (_predicate((_highestPriority, item)))
                    {
                        _highestPriority = item;
                        _highestIndex = i;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Clears the collection.
        /// </summary>
        public void Clear()
            => _collection.Clear();

        /// <summary>
        /// How many items are in the collection.
        /// </summary>
        public int Count
            => _collection.Count;

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => _collection.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => _collection.GetEnumerator();
    }
}
