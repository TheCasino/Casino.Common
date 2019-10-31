using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Kommon.Entities
{
    /// <summary>
    /// A wrapper of HashSet that locks its methods.
    /// </summary>
    /// <typeparam name="T">The type to use for the HashSet.</typeparam>
    public sealed class ConcurrentHashSet<T> : IReadOnlyCollection<T>, ISet<T>, IDeserializationCallback, ISerializable
    {
        private readonly HashSet<T> _hashSet;
        private readonly object _lock;

        public int Count
        {
            get
            {
                lock (_lock)
                    return _hashSet.Count;
            }
        }

        public IEqualityComparer<T> Comparer
        {
            get
            {
                lock (_lock)
                    return _hashSet.Comparer;
            }
        }

        public ConcurrentHashSet() : this(0, null, EqualityComparer<T>.Default)
        {
        }

        public ConcurrentHashSet(int capacity) : this(capacity, null, EqualityComparer<T>.Default)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        public ConcurrentHashSet(IEqualityComparer<T> comparer) : this(0, null, comparer)
        {
            if (comparer is null)
                throw new ArgumentNullException(nameof(comparer));
        }

        public ConcurrentHashSet(IEnumerable<T> collection) : this(-1, collection, EqualityComparer<T>.Default)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));
        }

        public ConcurrentHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer) : this(-1, collection, comparer)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            if (comparer is null)
                throw new ArgumentNullException(nameof(comparer));
        }

        public ConcurrentHashSet(int capacity, IEqualityComparer<T> comparer) : this(capacity, null, comparer)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            if (comparer is null)
                throw new ArgumentNullException(nameof(comparer));
        }

        private ConcurrentHashSet(int capacitity, IEnumerable<T> collection, IEqualityComparer<T> comparer)
        {
            if (collection is null)
                _hashSet = new HashSet<T>(capacitity, comparer);
            else
                _hashSet = new HashSet<T>(collection, comparer);

            _lock = new object();
        }

        public bool Add(T item)
        {
            lock (_lock)
                return _hashSet.Add(item);
        }

        public void Clear()
        {
            lock (_lock)
                _hashSet.Clear();
        }

        public bool Remove(T item)
        {
            lock (_lock)
                return _hashSet.Remove(item);
        }

        public bool Contains(T item)
        {
            lock (_lock)
                return _hashSet.Contains(item);
        }

        public void CopyTo(T[] array)
            => CopyTo(array, 0, Count);

        public void CopyTo(T[] array, int arrayIndex)
            => CopyTo(array, arrayIndex, Count);

        public void CopyTo(T[] array, int arrayIndex, int count)
        {
            lock (_lock)
                _hashSet.CopyTo(array, arrayIndex, count);
        }

        public int EnsureCapacity(int capacity)
        {
            lock (_lock)
                return _hashSet.EnsureCapacity(capacity);
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            lock (_lock)
                _hashSet.ExceptWith(other);
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            lock (_lock)
                _hashSet.SymmetricExceptWith(other);
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            lock (_lock)
                _hashSet.IntersectWith(other);
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            lock (_lock)
                return _hashSet.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            lock (_lock)
                return _hashSet.IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            lock (_lock)
                return _hashSet.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            lock (_lock)
                return _hashSet.IsSupersetOf(other);
        }        

        public bool Overlaps(IEnumerable<T> other)
        {
            lock (_lock)
                return _hashSet.Overlaps(other);
        }

        public int RemoveWhere(Predicate<T> match)
        {
            lock (_lock)
                return _hashSet.RemoveWhere(match);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            lock (_lock)
                return _hashSet.SetEquals(other);
        }

        public void TrimExcess()
        {
            lock (_lock)
                _hashSet.TrimExcess();
        }

        public bool TryGetValue(T equalValue, out T actualValue)
        {
            lock (_lock)
                return _hashSet.TryGetValue(equalValue, out actualValue);
        }

        public void UnionWith(IEnumerable<T> other)
        {
            lock (_lock)
                _hashSet.UnionWith(other);
        }

        void ICollection<T>.Add(T item)
            => Add(item);

        void ICollection<T>.Clear()
            => Clear();

        bool ICollection<T>.Contains(T item)
            => Contains(item);

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
            => CopyTo(array, arrayIndex);

        bool ICollection<T>.Remove(T item)
            => Remove(item);

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            lock (_lock)
                return _hashSet.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (_lock)
                return _hashSet.GetEnumerator();
        }

        bool ICollection<T>.IsReadOnly => false;

        int ICollection<T>.Count => Count;

        void IDeserializationCallback.OnDeserialization(object sender)
        {
            lock (_lock)
                _hashSet.OnDeserialization(sender);
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            lock (_lock)
                _hashSet.GetObjectData(info, context);
        }
    }
}
