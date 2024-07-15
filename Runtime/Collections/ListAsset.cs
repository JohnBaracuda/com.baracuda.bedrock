using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Baracuda.Bedrock.Odin;
using Baracuda.Utilities.Events;
using Sirenix.OdinInspector;

namespace Baracuda.Bedrock.Collections
{
    public abstract class ListAsset<T> : RuntimeCollectionAsset<T>, IList<T>, IReadOnlyList<T>
    {
        [ReadOnly]
        [ShowInInspector]
        [Foldout("Elements")]
        private readonly List<T> _list = new(16);
        private readonly Broadcast _changed = new();
        private readonly Broadcast<T> _removed = new();
        private readonly Broadcast<T> _added = new();

        /// <summary>
        ///     Called when the collection was altered.
        /// </summary>
        public event Action Changed
        {
            add => _changed.Add(value);
            remove => _changed.Remove(value);
        }

        /// <summary>
        ///     Called when an element was removed from the list.
        /// </summary>
        public event Action<T> Removed
        {
            add => _removed.Add(value);
            remove => _removed.Remove(value);
        }

        /// <summary>
        ///     Called when an element was added to the list.
        /// </summary>
        public event Action<T> Added
        {
            add => _added.Add(value);
            remove => _added.Remove(value);
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<T>.Enumerator GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        /// <summary>Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1" />.</summary>
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <exception cref="T:System.NotSupportedException">
        ///     The <see cref="T:System.Collections.Generic.ICollection`1" /> is
        ///     read-only.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            _list.Add(item);
            _changed.Raise();
            _added.Raise(item);
            Repaint();
        }

        /// <summary>
        ///     Adds the elements of the specified collection to the end of the
        ///     <see cref="T:System.Collections.Generic.List`1" />.
        /// </summary>
        /// <param name="collection">
        ///     The collection whose elements should be added to the end of the <see cref="T:System.Collections.Generic.List`1" />.
        ///     The collection itself cannot be <see langword="null" />, but it can contain elements that are
        ///     <see langword="null" />, if type is a reference type.
        /// </param>
        /// <exception cref="T:System.ArgumentNullException">
        ///     <paramref name="collection" /> is <see langword="null" />.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(IEnumerable<T> collection)
        {
            if (_added.Count > 0)
            {
                foreach (var item in collection)
                {
                    _list.Add(item);
                    _added.Raise(item);
                }
            }
            else
            {
                _list.AddRange(collection);
            }
            _changed.Raise();
            Repaint();
        }

        /// <summary>Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1" />.</summary>
        /// <exception cref="T:System.NotSupportedException">
        ///     The <see cref="T:System.Collections.Generic.ICollection`1" /> is
        ///     read-only.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _list.Clear();
            if (_removed.Count > 0)
            {
                foreach (var item in this)
                {
                    _removed.Raise(item);
                }
            }
            _changed.Raise();
            Repaint();
        }

        /// <summary>Determines whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains a specific value.</summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>
        ///     <see langword="true" /> if <paramref name="item" /> is found in the
        ///     <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, <see langword="false" />.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public bool Contains(T item)
        {
            return _list.Contains(item);
        }

        /// <summary>
        ///     Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1" /> to an
        ///     <see cref="T:System.Array" />, starting at a particular <see cref="T:System.Array" /> index.
        /// </summary>
        /// <param name="array">
        ///     The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied
        ///     from <see cref="T:System.Collections.Generic.ICollection`1" />. The <see cref="T:System.Array" /> must have
        ///     zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///     <paramref name="array" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///     <paramref name="arrayIndex" /> is less than 0.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        ///     The number of elements in the source
        ///     <see cref="T:System.Collections.Generic.ICollection`1" /> is greater than the available space from
        ///     <paramref name="arrayIndex" /> to the end of the destination <paramref name="array" />.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        /// <summary>
        ///     Removes the first occurrence of a specific object from the
        ///     <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>
        ///     <see langword="true" /> if <paramref name="item" /> was successfully removed from the
        ///     <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, <see langword="false" />. This method also
        ///     returns <see langword="false" /> if <paramref name="item" /> is not found in the original
        ///     <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">
        ///     The <see cref="T:System.Collections.Generic.ICollection`1" /> is
        ///     read-only.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(T item)
        {
            var result = _list.Remove(item);
            if (result)
            {
                _removed.Raise(item);
            }
            _changed.Raise();
            Repaint();
            return result;
        }

        /// <summary>Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.</summary>
        /// <returns>The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.</returns>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _list.Count;
        }

        /// <summary>Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.</summary>
        /// <returns>
        ///     <see langword="true" /> if the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only; otherwise,
        ///     <see langword="false" />.
        /// </returns>
        public bool IsReadOnly
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => false;
        }

        /// <summary>Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1" />.</summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1" />.</param>
        /// <returns>The index of <paramref name="item" /> if found in the list; otherwise, -1.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public int IndexOf(T item)
        {
            return _list.IndexOf(item);
        }

        /// <summary>Inserts an item to the <see cref="T:System.Collections.Generic.IList`1" /> at the specified index.</summary>
        /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
        /// <param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1" />.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///     <paramref name="index" /> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1" />.
        /// </exception>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IList`1" /> is read-only.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Insert(int index, T item)
        {
            _list.Insert(index, item);
            _added.Raise(item);
            _changed.Raise();
            Repaint();
        }

        /// <summary>Removes the <see cref="T:System.Collections.Generic.IList`1" /> item at the specified index.</summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///     <paramref name="index" /> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1" />.
        /// </exception>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IList`1" /> is read-only.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAt(int index)
        {
            if (_removed.Count > 0)
            {
                var element = _list[index];
                _removed.Raise(element);
            }
            _list.RemoveAt(index);
            _changed.Raise();
            Repaint();
        }

        /// <summary>Gets or sets the element at the specified index.</summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///     <paramref name="index" /> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1" />.
        /// </exception>
        /// <exception cref="T:System.NotSupportedException">
        ///     The property is set and the
        ///     <see cref="T:System.Collections.Generic.IList`1" /> is read-only.
        /// </exception>
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _list[index];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                _list[index] = value;
                _changed.Raise();
            }
        }

        /// <summary>
        ///     Sort the list using a default implementation.
        /// </summary>
        public void Sort()
        {
            _list.Sort();
            _changed.Raise();
        }

        /// <summary>
        ///     Sort the list using the passed comparison delegate.
        /// </summary>
        public void Sort(Comparison<T> comparison)
        {
            _list.Sort(comparison);
            _changed.Raise();
        }

        /// <summary>
        ///     Internal call to get the underlying collection.
        /// </summary>
        private protected sealed override IEnumerable<T> CollectionInternal
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _list;
        }

        /// <summary>
        ///     Internal call to clear the underlying collection.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected sealed override void ClearInternal()
        {
            Clear();
        }

        /// <summary>
        ///     Internal call to get the count of the underlying collection.
        /// </summary>
        private protected sealed override int CountInternal
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Count;
        }
    }
}