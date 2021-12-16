namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// Borrowed with thanks from <see href="https://github.com/StephenClearyArchive/Deque" />, and modified to be more netcore and provide queue manipulations required by the Terraform HCL emitter.
    /// </summary>
    /// <typeparam name="T">The type of elements contained in the queue.</typeparam>
    [DebuggerDisplay("Count = {Count}, Capacity = {Capacity}")]
    [DebuggerTypeProxy(typeof(EmitterEventQueue<>.DebugView))]
    internal sealed class EmitterEventQueue<T> : IList<T>, IList
    {
        /// <summary>
        /// The default capacity.
        /// </summary>
        private const int DefaultCapacity = 8;

        /// <summary>
        /// The circular buffer that holds the view.
        /// </summary>
        private T[] buffer;

        /// <summary>
        /// The offset into <see cref="buffer"/> where the view begins.
        /// </summary>
        private int offset;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmitterEventQueue{T}"/> class with the specified capacity.
        /// </summary>
        /// <param name="capacity">The initial capacity. Must be greater than <c>0</c>.</param>
        public EmitterEventQueue(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than 0.");
            }

            this.buffer = new T[capacity];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EmitterEventQueue{T}"/> class with the elements from the specified collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        public EmitterEventQueue(IEnumerable<T> collection)
        {
            var count = collection.Count();

            if (count > 0)
            {
                this.buffer = new T[count];
                this.DoInsertRange(0, collection, count);
            }
            else
            {
                this.buffer = new T[DefaultCapacity];
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EmitterEventQueue{T}"/> class.
        /// </summary>
        public EmitterEventQueue()
            : this(DefaultCapacity)
        {
        }

        /// <summary>
        /// Gets or sets the capacity for this queue. This value must always be greater than zero, and this property cannot be set to a value less than <see cref="Count"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"><c>Capacity</c> cannot be set to a value less than <see cref="Count"/>.</exception>
        public int Capacity
        {
            get => this.buffer.Length;

            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Capacity must be greater than 0.");
                }

                if (value < this.Count)
                {
                    throw new InvalidOperationException("Capacity cannot be set to a value less than Count");
                }

                if (value == this.buffer.Length)
                {
                    return;
                }

                // Create the new buffer and copy our existing range.
                var newBuffer = new T[value];

                if (this.IsSplit)
                {
                    // The existing buffer is split, so we have to copy it in parts
                    var length = this.Capacity - this.offset;
                    Array.Copy(this.buffer, this.offset, newBuffer, 0, length);
                    Array.Copy(this.buffer, 0, newBuffer, length, this.Count - length);
                }
                else
                {
                    // The existing buffer is whole
                    Array.Copy(this.buffer, this.offset, newBuffer, 0, this.Count);
                }

                // Set up to use the new buffer.
                this.buffer = newBuffer;
                this.offset = 0;
            }
        }

        /// <summary>
        /// Gets the number of elements contained in this queue.
        /// </summary>
        /// <returns>The number of elements contained in this queue.</returns>
        public int Count { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.IList"></see> has a fixed size.
        /// </summary>
        bool IList.IsFixedSize => false;

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"></see> is read-only.
        /// </summary>
        bool IList.IsReadOnly => false;

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection"></see> is synchronized (thread safe).
        /// </summary>
        bool ICollection.IsSynchronized => false;

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"></see>.
        /// </summary>
        object ICollection.SyncRoot => this;

        /// <summary>
        /// Gets a value indicating whether this list is read-only. This implementation always returns <c>false</c>.
        /// </summary>
        /// <returns>true if this list is read-only; otherwise, false.</returns>
        bool ICollection<T>.IsReadOnly => false;

        /// <summary>
        /// Gets a value indicating whether this instance is empty.
        /// </summary>
        private bool IsEmpty => this.Count == 0;

        /// <summary>
        /// Gets a value indicating whether this instance is at full capacity.
        /// </summary>
        private bool IsFull => this.Count == this.Capacity;

        /// <summary>
        /// Gets a value indicating whether the buffer is "split" (meaning the beginning of the view is at a later index in <see cref="buffer"/> than the end).
        /// </summary>
        private bool IsSplit =>

            // Overflow-safe version of "(offset + Count) > Capacity"
            this.offset > (this.Capacity - this.Count);

        /// <summary>
        /// Gets or sets the item at the specified index.
        /// </summary>
        /// <param name="index">The index of the item to get or set.</param>
        /// <returns>Item at the specified index.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in this list.</exception>
        /// <exception cref="T:System.NotSupportedException">This property is set and the list is read-only.</exception>
        public T this[int index]
        {
            get
            {
                CheckExistingIndexArgument(this.Count, index, true);
                return this.DoGetItem(index);
            }

            set
            {
                CheckExistingIndexArgument(this.Count, index, true);
                this.DoSetItem(index, value);
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Object"/> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="System.Object"/>.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns>Object at the specified index.</returns>
        /// <exception cref="System.ArgumentException">Item is not of the correct type. - value</exception>
        object IList.this[int index]
        {
            get => this[index];

            set
            {
                if (!this.ObjectIsT(value))
                {
                    throw new ArgumentException("Item is not of the correct type.", nameof(value));
                }

                this[index] = (T)value;
            }
        }

        /// <summary>
        /// Inserts a single element at the back of this queue.
        /// </summary>
        /// <param name="value">The element to insert.</param>
        public void AddToBack(T value)
        {
            this.EnsureCapacityForOneElement();
            this.DoAddToBack(value);
        }

        /// <summary>
        /// Removes all items from this queue.
        /// </summary>
        public void Clear()
        {
            this.offset = 0;
            this.Count = 0;
        }

        /// <summary>
        /// Consume items from front of queue until a condition is met.
        /// </summary>
        /// <param name="untilFunc">The until function.</param>
        /// <param name="consumeLastItem">If <c>true</c> consume the item that ends the iteration.</param>
        public void ConsumeUntil(Func<T, bool> untilFunc, bool consumeLastItem)
        {
            while (true)
            {
                var item = this.Peek();

                if (untilFunc(item))
                {
                    if (consumeLastItem)
                    {
                        this.Dequeue();
                    }

                    break;
                }

                this.Dequeue();
            }
        }

        /// <summary>
        /// Removes and returns the first element of this queue (standard queue interface)
        /// </summary>
        /// <returns>Item at front of queue</returns>
        public T Dequeue()
        {
            return this.RemoveFromFront();
        }

        /// <summary>
        /// Inserts a single element at the back of this queue (standard queue interface)
        /// </summary>
        /// <param name="value">The value.</param>
        public void Enqueue(T value)
        {
            this.AddToBack(value);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<T> GetEnumerator()
        {
            var count = this.Count;
            for (var i = 0; i != count; ++i)
            {
                yield return this.DoGetItem(i);
            }
        }

        /// <summary>
        /// Determines the index of a specific item in this list.
        /// </summary>
        /// <param name="item">The object to locate in this list.</param>
        /// <returns>The index of <paramref name="item"/> if found in this list; otherwise, -1.</returns>
        public int IndexOf(T item)
        {
            var comparer = EqualityComparer<T>.Default;
            var ret = 0;
            foreach (var sourceItem in this)
            {
                if (comparer.Equals(item, sourceItem))
                {
                    return ret;
                }

                ++ret;
            }

            return -1;
        }

        /// <summary>
        /// Inserts an item to this list at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The object to insert into this list.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="index"/> is not a valid index in this list.
        /// </exception>
        /// <exception cref="T:System.NotSupportedException">
        /// This list is read-only.
        /// </exception>
        public void Insert(int index, T item)
        {
            CheckNewIndexArgument(this.Count, index);
            this.DoInsert(index, item);
        }

        /// <summary>
        /// Peeks the item the front of the queue.
        /// </summary>
        /// <returns>Item at front of queue</returns>
        public T Peek()
        {
            return this.PeekFront();
        }

        /// <summary>
        /// Peeks the item the front of the queue.
        /// </summary>
        /// <returns>Item at front of queue</returns>
        public T PeekFront()
        {
            CheckExistingIndexArgument(this.Count, 0, true);
            return this.DoGetItem(0);
        }

        /// <summary>
        /// Peek items until a condition is met.
        /// </summary>
        /// <param name="untilFunc">The until function.</param>
        /// <param name="emitLastItem">If <c>true</c> emit the item that ends the iteration.</param>
        /// <returns>Enumerable of items peeked.</returns>
        public IEnumerable<T> PeekUntil(Func<T, bool> untilFunc, bool emitLastItem)
        {
            for (var i = 0; i <= this.Count; ++i)
            {
                var item = this[i];

                if (untilFunc(item))
                {
                    if (emitLastItem)
                    {
                        yield return item;
                    }

                    yield break;
                }

                yield return item;
            }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from this list.
        /// </summary>
        /// <param name="item">The object to remove from this list.</param>
        /// <returns>
        /// true if <paramref name="item"/> was successfully removed from this list; otherwise, false. This method also returns false if <paramref name="item"/> is not found in this list.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">
        /// This list is read-only.
        /// </exception>
        public bool Remove(T item)
        {
            var index = this.IndexOf(item);
            if (index == -1)
            {
                return false;
            }

            this.DoRemoveAt(index);
            return true;
        }

        /// <summary>
        /// Removes the item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="index"/> is not a valid index in this list.
        /// </exception>
        /// <exception cref="T:System.NotSupportedException">
        /// This list is read-only.
        /// </exception>
        public void RemoveAt(int index)
        {
            CheckExistingIndexArgument(this.Count, index, true);
            this.DoRemoveAt(index);
        }

        /// <summary>
        /// Removes and returns the first element of this queue.
        /// </summary>
        /// <returns>The former first element.</returns>
        /// <exception cref="InvalidOperationException">The queue is empty.</exception>
        public T RemoveFromFront()
        {
            if (this.IsEmpty)
            {
                throw new InvalidOperationException("The queue is empty.");
            }

            return this.DoRemoveFromFront();
        }

        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.IList"></see>.
        /// </summary>
        /// <param name="value">The object to add to the <see cref="T:System.Collections.IList"></see>.</param>
        /// <returns>
        /// The position into which the new element was inserted, or -1 to indicate that the item was not inserted into the collection.
        /// </returns>
        /// <exception cref="System.ArgumentException">Item is not of the correct type. - value</exception>
        int IList.Add(object value)
        {
            if (!this.ObjectIsT(value))
            {
                throw new ArgumentException("Item is not of the correct type.", nameof(value));
            }

            this.AddToBack((T)value);
            return this.Count - 1;
        }

        /// <summary>
        /// Determines whether this instance contains the object.
        /// </summary>
        /// <param name="value">The object to locate in the <see cref="T:System.Collections.IList"></see>.</param>
        /// <returns>
        /// true if the <see cref="T:System.Object"></see> is found in the <see cref="T:System.Collections.IList"></see>; otherwise, false.
        /// </returns>
        /// <exception cref="System.ArgumentException">Item is not of the correct type. - value</exception>
        bool IList.Contains(object value)
        {
            if (!this.ObjectIsT(value))
            {
                throw new ArgumentException("Item is not of the correct type.", nameof(value));
            }

            return this.Contains((T)value);
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.ICollection"></see> to an <see cref="T:System.Array"></see>, starting at a particular <see cref="T:System.Array"></see> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"></see> that is the destination of the elements copied from <see cref="T:System.Collections.ICollection"></see>. The <see cref="T:System.Array"></see> must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in array at which copying begins.</param>
        /// <exception cref="System.ArgumentNullException">array - Destination array cannot be null.</exception>
        /// <exception cref="System.ArgumentException">Destination array is of incorrect type.</exception>
        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array), "Destination array cannot be null.");
            }

            CheckRangeArguments(array.Length, index, this.Count);

            for (var i = 0; i != this.Count; ++i)
            {
                try
                {
                    array.SetValue(this[i], index + i);
                }
                catch (InvalidCastException ex)
                {
                    throw new ArgumentException("Destination array is of incorrect type.", ex);
                }
            }
        }

        /// <summary>
        /// Determines the index of a specific item in the <see cref="T:System.Collections.IList"></see>.
        /// </summary>
        /// <param name="value">The object to locate in the <see cref="T:System.Collections.IList"></see>.</param>
        /// <returns>
        /// The index of <paramref name="value">value</paramref> if found in the list; otherwise, -1.
        /// </returns>
        /// <exception cref="System.ArgumentException">Item is not of the correct type. - value</exception>
        int IList.IndexOf(object value)
        {
            if (!this.ObjectIsT(value))
            {
                throw new ArgumentException("Item is not of the correct type.", nameof(value));
            }

            return this.IndexOf((T)value);
        }

        /// <summary>
        /// Inserts an item to the <see cref="T:System.Collections.IList"></see> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which value should be inserted.</param>
        /// <param name="value">The object to insert into the <see cref="T:System.Collections.IList"></see>.</param>
        /// <exception cref="System.ArgumentException">Item is not of the correct type. - value</exception>
        void IList.Insert(int index, object value)
        {
            if (!this.ObjectIsT(value))
            {
                throw new ArgumentException("Item is not of the correct type.", nameof(value));
            }

            this.Insert(index, (T)value);
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.IList"></see>.
        /// </summary>
        /// <param name="value">The object to remove from the <see cref="T:System.Collections.IList"></see>.</param>
        /// <exception cref="System.ArgumentException">Item is not of the correct type. - value</exception>
        void IList.Remove(object value)
        {
            if (!this.ObjectIsT(value))
            {
                throw new ArgumentException("Item is not of the correct type.", nameof(value));
            }

            this.Remove((T)value);
        }

        /// <summary>
        /// Adds an item to the end of this list.
        /// </summary>
        /// <param name="item">The object to add to this list.</param>
        /// <exception cref="T:System.NotSupportedException">
        /// This list is read-only.
        /// </exception>
        void ICollection<T>.Add(T item)
        {
            this.DoInsert(this.Count, item);
        }

        /// <summary>
        /// Determines whether this list contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in this list.</param>
        /// <returns>
        /// true if <paramref name="item"/> is found in this list; otherwise, false.
        /// </returns>
        bool ICollection<T>.Contains(T item)
        {
            return this.Contains(item, null);
        }

        /// <summary>
        /// Copies the elements of this list to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from this slice. The <see cref="T:System.Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="array"/> is null.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="arrayIndex"/> is less than 0.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="arrayIndex"/> is equal to or greater than the length of <paramref name="array"/>.
        /// -or-
        /// The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.
        /// </exception>
        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array), "Array is null");
            }

            var count = this.Count;
            CheckRangeArguments(array.Length, arrayIndex, count);
            for (var i = 0; i != count; ++i)
            {
                array[arrayIndex + i] = this[i];
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Checks the <paramref name="index"/> argument to see if it refers to an existing element in a source of a given length.
        /// </summary>
        /// <param name="sourceLength">The length of the source. This parameter is not checked for validity.</param>
        /// <param name="index">The index into the source.</param>
        /// <param name="throwIfOutOfRange">If <c>true</c>, throw exception when index out of range</param>
        /// <returns><c>true</c> if index is in range; else <c>false</c> if <paramref name="throwIfOutOfRange"/> is <c>false</c></returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index to an existing element for the source.</exception>
        private static bool CheckExistingIndexArgument(int sourceLength, int index, bool throwIfOutOfRange)
        {
            if (index < 0 || index >= sourceLength)
            {
                if (throwIfOutOfRange)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(index),
                        "Invalid existing index " + index + " for source length " + sourceLength);
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks the <paramref name="index"/> argument to see if it refers to a valid insertion point in a source of a given length.
        /// </summary>
        /// <param name="sourceLength">The length of the source. This parameter is not checked for validity.</param>
        /// <param name="index">The index into the source.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index to an insertion point for the source.</exception>
        private static void CheckNewIndexArgument(int sourceLength, int index)
        {
            if (index < 0 || index > sourceLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    "Invalid new index " + index + " for source length " + sourceLength);
            }
        }

        /// <summary>
        /// Checks the <paramref name="offset"/> and <paramref name="count"/> arguments for validity when applied to a source of a given length. Allows 0-element ranges, including a 0-element range at the end of the source.
        /// </summary>
        /// <param name="sourceLength">The length of the source. This parameter is not checked for validity.</param>
        /// <param name="offset">The index into source at which the range begins.</param>
        /// <param name="count">The number of elements in the range.</param>
        /// <exception cref="ArgumentOutOfRangeException">Either <paramref name="offset"/> or <paramref name="count"/> is less than 0.</exception>
        /// <exception cref="ArgumentException">The range [offset, offset + count) is not within the range [0, sourceLength).</exception>
        private static void CheckRangeArguments(int sourceLength, int offset, int count)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "Invalid offset " + offset);
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Invalid count " + count);
            }

            if (sourceLength - offset < count)
            {
                throw new ArgumentException(
                    "Invalid offset (" + offset + ") or count + (" + count + ") for source length " + sourceLength);
            }
        }

        /// <summary>
        /// Applies the offset to <paramref name="index"/>, resulting in a buffer index.
        /// </summary>
        /// <param name="index">The queue index.</param>
        /// <returns>The buffer index.</returns>
        private int DequeIndexToBufferIndex(int index)
        {
            return (index + this.offset) % this.Capacity;
        }

        /// <summary>
        /// Inserts a single element to the back of the view. <see cref="IsFull"/> must be false when this method is called.
        /// </summary>
        /// <param name="value">The element to insert.</param>
        private void DoAddToBack(T value)
        {
            this.buffer[this.DequeIndexToBufferIndex(this.Count)] = value;
            ++this.Count;
        }

        /// <summary>
        /// Inserts a single element to the front of the view. <see cref="IsFull"/> must be false when this method is called.
        /// </summary>
        /// <param name="value">The element to insert.</param>
        private void DoAddToFront(T value)
        {
            this.buffer[this.PreDecrement(1)] = value;
            ++this.Count;
        }

        /// <summary>
        /// Gets an element at the specified view index.
        /// </summary>
        /// <param name="index">The zero-based view index of the element to get. This index is guaranteed to be valid.</param>
        /// <returns>The element at the specified index.</returns>
        private T DoGetItem(int index)
        {
            return this.buffer[this.DequeIndexToBufferIndex(index)];
        }

        /// <summary>
        /// Inserts an element at the specified view index.
        /// </summary>
        /// <param name="index">The zero-based view index at which the element should be inserted. This index is guaranteed to be valid.</param>
        /// <param name="item">The element to store in the list.</param>
        private void DoInsert(int index, T item)
        {
            this.EnsureCapacityForOneElement();

            if (index == 0)
            {
                this.DoAddToFront(item);
                return;
            }
            else if (index == this.Count)
            {
                this.DoAddToBack(item);
                return;
            }

            this.DoInsertRange(index, new[] { item }, 1);
        }

        /// <summary>
        /// Inserts a range of elements into the view.
        /// </summary>
        /// <param name="index">The index into the view at which the elements are to be inserted.</param>
        /// <param name="collection">The elements to insert.</param>
        /// <param name="collectionCount">The number of elements in <paramref name="collection"/>. Must be greater than zero, and the sum of <paramref name="collectionCount"/> and <see cref="Count"/> must be less than or equal to <see cref="Capacity"/>.</param>
        private void DoInsertRange(int index, IEnumerable<T> collection, int collectionCount)
        {
            // Make room in the existing list
            if (index < this.Count / 2)
            {
                // Inserting into the first half of the list

                // Move lower items down: [0, index) -> [Capacity - collectionCount, Capacity - collectionCount + index)
                // This clears out the low "index" number of items, moving them "collectionCount" places down;
                // after rotation, there will be a "collectionCount"-sized hole at "index".
                var copyCount = index;
                var writeIndex = this.Capacity - collectionCount;
                for (var j = 0; j != copyCount; ++j)
                {
                    this.buffer[this.DequeIndexToBufferIndex(writeIndex + j)] =
                        this.buffer[this.DequeIndexToBufferIndex(j)];
                }

                // Rotate to the new view
                this.PreDecrement(collectionCount);
            }
            else
            {
                // Inserting into the second half of the list

                // Move higher items up: [index, count) -> [index + collectionCount, collectionCount + count)
                var copyCount = this.Count - index;
                var writeIndex = index + collectionCount;
                for (var j = copyCount - 1; j != -1; --j)
                {
                    this.buffer[this.DequeIndexToBufferIndex(writeIndex + j)] =
                        this.buffer[this.DequeIndexToBufferIndex(index + j)];
                }
            }

            // Copy new items into place
            var i = index;
            foreach (var item in collection)
            {
                this.buffer[this.DequeIndexToBufferIndex(i)] = item;
                ++i;
            }

            // Adjust valid count
            this.Count += collectionCount;
        }

        /// <summary>
        /// Removes an element at the specified view index.
        /// </summary>
        /// <param name="index">The zero-based view index of the element to remove. This index is guaranteed to be valid.</param>
        private void DoRemoveAt(int index)
        {
            if (index == 0)
            {
                this.DoRemoveFromFront();
                return;
            }
            else if (index == this.Count - 1)
            {
                this.DoRemoveFromBack();
                return;
            }

            this.DoRemoveRange(index, 1);
        }

        /// <summary>
        /// Removes and returns the last element in the view. <see cref="IsEmpty"/> must be false when this method is called.
        /// </summary>
        /// <returns>The former last element.</returns>
        private T DoRemoveFromBack()
        {
            var ret = this.buffer[this.DequeIndexToBufferIndex(this.Count - 1)];
            --this.Count;
            return ret;
        }

        /// <summary>
        /// Removes and returns the first element in the view. <see cref="IsEmpty"/> must be false when this method is called.
        /// </summary>
        /// <returns>The former first element.</returns>
        private T DoRemoveFromFront()
        {
            --this.Count;
            return this.buffer[this.PostIncrement(1)];
        }

        /// <summary>
        /// Removes a range of elements from the view.
        /// </summary>
        /// <param name="index">The index into the view at which the range begins.</param>
        /// <param name="collectionCount">The number of elements in the range. This must be greater than 0 and less than or equal to <see cref="Count"/>.</param>
        private void DoRemoveRange(int index, int collectionCount)
        {
            if (index == 0)
            {
                // Removing from the beginning: rotate to the new view
                this.PostIncrement(collectionCount);
                this.Count -= collectionCount;
                return;
            }
            else if (index == this.Count - collectionCount)
            {
                // Removing from the ending: trim the existing view
                this.Count -= collectionCount;
                return;
            }

            if ((index + (collectionCount / 2)) < this.Count / 2)
            {
                // Removing from first half of list

                // Move lower items up: [0, index) -> [collectionCount, collectionCount + index)
                var copyCount = index;
                var writeIndex = collectionCount;
                for (var j = copyCount - 1; j != -1; --j)
                {
                    this.buffer[this.DequeIndexToBufferIndex(writeIndex + j)] =
                        this.buffer[this.DequeIndexToBufferIndex(j)];
                }

                // Rotate to new view
                this.PostIncrement(collectionCount);
            }
            else
            {
                // Removing from second half of list

                // Move higher items down: [index + collectionCount, count) -> [index, count - collectionCount)
                var copyCount = this.Count - collectionCount - index;
                var readIndex = index + collectionCount;
                for (var j = 0; j != copyCount; ++j)
                {
                    this.buffer[this.DequeIndexToBufferIndex(index + j)] =
                        this.buffer[this.DequeIndexToBufferIndex(readIndex + j)];
                }
            }

            // Adjust valid count
            this.Count -= collectionCount;
        }

        /// <summary>
        /// Sets an element at the specified view index.
        /// </summary>
        /// <param name="index">The zero-based view index of the element to get. This index is guaranteed to be valid.</param>
        /// <param name="item">The element to store in the list.</param>
        private void DoSetItem(int index, T item)
        {
            this.buffer[this.DequeIndexToBufferIndex(index)] = item;
        }

        /// <summary>
        /// Doubles the capacity if necessary to make room for one more element. When this method returns, <see cref="IsFull"/> is false.
        /// </summary>
        private void EnsureCapacityForOneElement()
        {
            if (this.IsFull)
            {
                this.Capacity = this.Capacity * 2;
            }
        }

        /// <summary>
        /// Returns whether or not the type of a given item indicates it is appropriate for storing in this container.
        /// </summary>
        /// <param name="item">The item to test.</param>
        /// <returns><c>true</c> if the item is appropriate to store in this container; otherwise, <c>false</c>.</returns>
        private bool ObjectIsT(object item)
        {
            switch (item)
            {
                case T _:
                    return true;

                case null:
                    {
                        var type = typeof(T);
                        if (type.IsClass && !type.IsPointer)
                        {
                            return true; // classes, arrays, and delegates
                        }

                        if (type.IsInterface)
                        {
                            return true; // interfaces
                        }

                        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            return true; // nullable value types
                        }

                        break;
                    }
            }

            return false;
        }

        /// <summary>
        /// Increments <see cref="offset"/> by <paramref name="value"/> using modulo-<see cref="Capacity"/> arithmetic.
        /// </summary>
        /// <param name="value">The value by which to increase <see cref="offset"/>. May not be negative.</param>
        /// <returns>The value of <see cref="offset"/> after it was incremented.</returns>
        private int PostIncrement(int value)
        {
            var ret = this.offset;
            this.offset += value;
            this.offset %= this.Capacity;
            return ret;
        }

        /// <summary>
        /// Decrements <see cref="offset"/> by <paramref name="value"/> using modulo-<see cref="Capacity"/> arithmetic.
        /// </summary>
        /// <param name="value">The value by which to reduce <see cref="offset"/>. May not be negative or greater than <see cref="Capacity"/>.</param>
        /// <returns>The value of <see cref="offset"/> before it was decremented.</returns>
        private int PreDecrement(int value)
        {
            this.offset -= value;
            if (this.offset < 0)
            {
                this.offset += this.Capacity;
            }

            return this.offset;
        }

        [DebuggerNonUserCode]
        private sealed class DebugView
        {
            private readonly EmitterEventQueue<T> queue;

            public DebugView(EmitterEventQueue<T> queue)
            {
                this.queue = queue;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public T[] Items
            {
                get
                {
                    var array = new T[this.queue.Count];
                    ((ICollection<T>)this.queue).CopyTo(array, 0);
                    return array;
                }
            }
        }
    }
}