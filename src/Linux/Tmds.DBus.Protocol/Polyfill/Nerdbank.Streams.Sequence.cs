// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.Streams
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Manages a sequence of elements, readily castable as a <see cref="ReadOnlySequence{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of element stored by the sequence.</typeparam>
    /// <remarks>
    /// Instance members are not thread-safe.
    /// </remarks>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    internal class Sequence<T> : IBufferWriter<T>, IDisposable
    {
        private const int MaximumAutoGrowSize = 32 * 1024;

        private static readonly int DefaultLengthFromArrayPool = 1 + (4095 / Unsafe.SizeOf<T>());

        private static readonly ReadOnlySequence<T> Empty = new ReadOnlySequence<T>(SequenceSegment.Empty, 0, SequenceSegment.Empty, 0);

        private readonly Stack<SequenceSegment> segmentPool = new Stack<SequenceSegment>();

        private readonly MemoryPool<T>? memoryPool;

        private readonly ArrayPool<T>? arrayPool;

        private SequenceSegment? first;

        private SequenceSegment? last;

        /// <summary>
        /// Initializes a new instance of the <see cref="Sequence{T}"/> class
        /// that uses a private <see cref="ArrayPool{T}"/> for recycling arrays.
        /// </summary>
        public Sequence()
            : this(ArrayPool<T>.Create())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sequence{T}"/> class.
        /// </summary>
        /// <param name="memoryPool">The pool to use for recycling backing arrays.</param>
        public Sequence(MemoryPool<T> memoryPool)
        {
            this.memoryPool = memoryPool ?? throw new ArgumentNullException(nameof(memoryPool));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sequence{T}"/> class.
        /// </summary>
        /// <param name="arrayPool">The pool to use for recycling backing arrays.</param>
        public Sequence(ArrayPool<T> arrayPool)
        {
            this.arrayPool = arrayPool ?? throw new ArgumentNullException(nameof(arrayPool));
        }

        /// <summary>
        /// Gets or sets the minimum length for any array allocated as a segment in the sequence.
        /// Any non-positive value allows the pool to determine the length of the array.
        /// </summary>
        /// <value>The default value is 0.</value>
        /// <remarks>
        /// <para>
        /// Each time <see cref="GetSpan(int)"/> or <see cref="GetMemory(int)"/> is called,
        /// previously allocated memory is used if it is large enough to satisfy the length demand.
        /// If new memory must be allocated, the argument to one of these methods typically dictate
        /// the length of array to allocate. When the caller uses very small values (just enough for its immediate need)
        /// but the high level scenario can predict that a large amount of memory will be ultimately required,
        /// it can be advisable to set this property to a value such that just a few larger arrays are allocated
        /// instead of many small ones.
        /// </para>
        /// <para>
        /// The <see cref="MemoryPool{T}"/> in use may itself have a minimum array length as well,
        /// in which case the higher of the two minimums dictate the minimum array size that will be allocated.
        /// </para>
        /// <para>
        /// If <see cref="AutoIncreaseMinimumSpanLength"/> is <see langword="true"/>, this value may be automatically increased as the length of a sequence grows.
        /// </para>
        /// </remarks>
        public int MinimumSpanLength { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="MinimumSpanLength"/> should be
        /// intelligently increased as the length of the sequence grows.
        /// </summary>
        /// <remarks>
        /// This can help prevent long sequences made up of many very small arrays.
        /// </remarks>
        public bool AutoIncreaseMinimumSpanLength { get; set; } = true;

        /// <summary>
        /// Gets this sequence expressed as a <see cref="ReadOnlySequence{T}"/>.
        /// </summary>
        /// <returns>A read only sequence representing the data in this object.</returns>
        public ReadOnlySequence<T> AsReadOnlySequence => this;

        /// <summary>
        /// Gets the length of the sequence.
        /// </summary>
        public long Length => this.AsReadOnlySequence.Length;

        /// <summary>
        /// Gets the value to display in a debugger datatip.
        /// </summary>
        private string DebuggerDisplay => $"Length: {this.AsReadOnlySequence.Length}";

        /// <summary>
        /// Expresses this sequence as a <see cref="ReadOnlySequence{T}"/>.
        /// </summary>
        /// <param name="sequence">The sequence to convert.</param>
        public static implicit operator ReadOnlySequence<T>(Sequence<T> sequence)
        {
            return sequence.first is { } first && sequence.last is { } last
                ? new ReadOnlySequence<T>(first, first.Start, last, last!.End)
                : Empty;
        }

        /// <summary>
        /// Removes all elements from the sequence from its beginning to the specified position,
        /// considering that data to have been fully processed.
        /// </summary>
        /// <param name="position">
        /// The position of the first element that has not yet been processed.
        /// This is typically <see cref="ReadOnlySequence{T}.End"/> after reading all elements from that instance.
        /// </param>
        public void AdvanceTo(SequencePosition position)
        {
            var firstSegment = (SequenceSegment?)position.GetObject();
            if (firstSegment == null)
            {
                // Emulate PipeReader behavior which is to just return for default(SequencePosition)
                return;
            }

            if (ReferenceEquals(firstSegment, SequenceSegment.Empty) && this.Length == 0)
            {
                // We were called with our own empty buffer segment.
                return;
            }

            int firstIndex = position.GetInteger();

            // Before making any mutations, confirm that the block specified belongs to this sequence.
            Sequence<T>.SequenceSegment? current = this.first;
            while (current != firstSegment && current != null)
            {
                current = current.Next;
            }

            if (current == null)
                throw new ArgumentException("Position does not represent a valid position in this sequence.",
                    nameof(position));
            
            // Also confirm that the position is not a prior position in the block.
            if (firstIndex < current.Start)
                throw new ArgumentException("Position must not be earlier than current position.", nameof(position));

            // Now repeat the loop, performing the mutations.
            current = this.first;
            while (current != firstSegment)
            {
                current = this.RecycleAndGetNext(current!);
            }

            firstSegment.AdvanceTo(firstIndex);

            this.first = firstSegment.Length == 0 ? this.RecycleAndGetNext(firstSegment) : firstSegment;

            if (this.first == null)
            {
                this.last = null;
            }
        }

        /// <summary>
        /// Advances the sequence to include the specified number of elements initialized into memory
        /// returned by a prior call to <see cref="GetMemory(int)"/>.
        /// </summary>
        /// <param name="count">The number of elements written into memory.</param>
        public void Advance(int count)
        {
            SequenceSegment? last = this.last;
            if(last==null)
                throw new InvalidOperationException("Cannot advance before acquiring memory.");
            last.Advance(count);
            this.ConsiderMinimumSizeIncrease();
        }

        /// <summary>
        /// Gets writable memory that can be initialized and added to the sequence via a subsequent call to <see cref="Advance(int)"/>.
        /// </summary>
        /// <param name="sizeHint">The size of the memory required, or 0 to just get a convenient (non-empty) buffer.</param>
        /// <returns>The requested memory.</returns>
        public Memory<T> GetMemory(int sizeHint) => this.GetSegment(sizeHint).RemainingMemory;

        /// <summary>
        /// Gets writable memory that can be initialized and added to the sequence via a subsequent call to <see cref="Advance(int)"/>.
        /// </summary>
        /// <param name="sizeHint">The size of the memory required, or 0 to just get a convenient (non-empty) buffer.</param>
        /// <returns>The requested memory.</returns>
        public Span<T> GetSpan(int sizeHint) => this.GetSegment(sizeHint).RemainingSpan;

        /// <summary>
        /// Adds an existing memory location to this sequence without copying.
        /// </summary>
        /// <param name="memory">The memory to add.</param>
        /// <remarks>
        /// This *may* leave significant slack space in a previously allocated block if calls to <see cref="Append(ReadOnlyMemory{T})"/>
        /// follow calls to <see cref="GetMemory(int)"/> or <see cref="GetSpan(int)"/>.
        /// </remarks>
        public void Append(ReadOnlyMemory<T> memory)
        {
            if (memory.Length > 0)
            {
                Sequence<T>.SequenceSegment? segment = this.segmentPool.Count > 0 ? this.segmentPool.Pop() : new SequenceSegment();
                segment.AssignForeign(memory);
                this.Append(segment);
            }
        }

        /// <summary>
        /// Clears the entire sequence, recycles associated memory into pools,
        /// and resets this instance for reuse.
        /// This invalidates any <see cref="ReadOnlySequence{T}"/> previously produced by this instance.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Dispose() => this.Reset();

        /// <summary>
        /// Clears the entire sequence and recycles associated memory into pools.
        /// This invalidates any <see cref="ReadOnlySequence{T}"/> previously produced by this instance.
        /// </summary>
        public void Reset()
        {
            Sequence<T>.SequenceSegment? current = this.first;
            while (current != null)
            {
                current = this.RecycleAndGetNext(current);
            }

            this.first = this.last = null;
        }

        private SequenceSegment GetSegment(int sizeHint)
        {
            if (sizeHint < 0)
                throw new ArgumentOutOfRangeException(nameof(sizeHint));
            int? minBufferSize = null;
            if (sizeHint == 0)
            {
                if (this.last == null || this.last.WritableBytes == 0)
                {
                    // We're going to need more memory. Take whatever size the pool wants to give us.
                    minBufferSize = -1;
                }
            }
            else
            {
                if (this.last == null || this.last.WritableBytes < sizeHint)
                {
                    minBufferSize = Math.Max(this.MinimumSpanLength, sizeHint);
                }
            }

            if (minBufferSize.HasValue)
            {
                Sequence<T>.SequenceSegment? segment = this.segmentPool.Count > 0 ? this.segmentPool.Pop() : new SequenceSegment();
                if (this.arrayPool != null)
                {
                    segment.Assign(this.arrayPool.Rent(minBufferSize.Value == -1 ? DefaultLengthFromArrayPool : minBufferSize.Value));
                }
                else
                {
                    segment.Assign(this.memoryPool!.Rent(minBufferSize.Value));
                }

                this.Append(segment);
            }

            return this.last!;
        }

        private void Append(SequenceSegment segment)
        {
            if (this.last == null)
            {
                this.first = this.last = segment;
            }
            else
            {
                if (this.last.Length > 0)
                {
                    // Add a new block.
                    this.last.SetNext(segment);
                }
                else
                {
                    // The last block is completely unused. Replace it instead of appending to it.
                    Sequence<T>.SequenceSegment? current = this.first;
                    if (this.first != this.last)
                    {
                        while (current!.Next != this.last)
                        {
                            current = current.Next;
                        }
                    }
                    else
                    {
                        this.first = segment;
                    }

                    current!.SetNext(segment);
                    this.RecycleAndGetNext(this.last);
                }

                this.last = segment;
            }
        }

        private SequenceSegment? RecycleAndGetNext(SequenceSegment segment)
        {
            Sequence<T>.SequenceSegment? recycledSegment = segment;
            Sequence<T>.SequenceSegment? nextSegment = segment.Next;
            recycledSegment.ResetMemory(this.arrayPool);
            this.segmentPool.Push(recycledSegment);
            return nextSegment;
        }

        private void ConsiderMinimumSizeIncrease()
        {
            if (this.AutoIncreaseMinimumSpanLength && this.MinimumSpanLength < MaximumAutoGrowSize)
            {
                int autoSize = Math.Min(MaximumAutoGrowSize, (int)Math.Min(int.MaxValue, this.Length / 2));
                if (this.MinimumSpanLength < autoSize)
                {
                    this.MinimumSpanLength = autoSize;
                }
            }
        }

        private class SequenceSegment : ReadOnlySequenceSegment<T>
        {
            internal static readonly SequenceSegment Empty = new SequenceSegment();

            /// <summary>
            /// A value indicating whether the element may contain references (and thus must be cleared).
            /// </summary>
            private static readonly bool MayContainReferences = !typeof(T).GetTypeInfo().IsPrimitive;

#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
            /// <summary>
            /// Gets the backing array, when using an <see cref="ArrayPool{T}"/> instead of a <see cref="MemoryPool{T}"/>.
            /// </summary>
            private T[]? array;
#pragma warning restore SA1011 // Closing square brackets should be spaced correctly

            /// <summary>
            /// Gets the position within <see cref="ReadOnlySequenceSegment{T}.Memory"/> where the data starts.
            /// </summary>
            /// <remarks>This may be nonzero as a result of calling <see cref="Sequence{T}.AdvanceTo(SequencePosition)"/>.</remarks>
            internal int Start { get; private set; }

            /// <summary>
            /// Gets the position within <see cref="ReadOnlySequenceSegment{T}.Memory"/> where the data ends.
            /// </summary>
            internal int End { get; private set; }

            /// <summary>
            /// Gets the tail of memory that has not yet been committed.
            /// </summary>
            internal Memory<T> RemainingMemory => this.AvailableMemory.Slice(this.End);

            /// <summary>
            /// Gets the tail of memory that has not yet been committed.
            /// </summary>
            internal Span<T> RemainingSpan => this.AvailableMemory.Span.Slice(this.End);

            /// <summary>
            /// Gets the tracker for the underlying array for this segment, which can be used to recycle the array when we're disposed of.
            /// Will be <see langword="null"/> if using an array pool, in which case the memory is held by <see cref="array"/>.
            /// </summary>
            internal IMemoryOwner<T>? MemoryOwner { get; private set; }

            /// <summary>
            /// Gets the full memory owned by the <see cref="MemoryOwner"/>.
            /// </summary>
            internal Memory<T> AvailableMemory => this.array ?? this.MemoryOwner?.Memory ?? default;

            /// <summary>
            /// Gets the number of elements that are committed in this segment.
            /// </summary>
            internal int Length => this.End - this.Start;

            /// <summary>
            /// Gets the amount of writable bytes in this segment.
            /// It is the amount of bytes between <see cref="Length"/> and <see cref="End"/>.
            /// </summary>
            internal int WritableBytes => this.AvailableMemory.Length - this.End;

            /// <summary>
            /// Gets or sets the next segment in the singly linked list of segments.
            /// </summary>
            internal new SequenceSegment? Next
            {
                get => (SequenceSegment?)base.Next;
                set => base.Next = value;
            }

            /// <summary>
            /// Gets a value indicating whether this segment refers to memory that came from outside and that we cannot write to nor recycle.
            /// </summary>
            internal bool IsForeignMemory => this.array == null && this.MemoryOwner == null;

            /// <summary>
            /// Assigns this (recyclable) segment a new area in memory.
            /// </summary>
            /// <param name="memoryOwner">The memory and a means to recycle it.</param>
            internal void Assign(IMemoryOwner<T> memoryOwner)
            {
                this.MemoryOwner = memoryOwner;
                this.Memory = memoryOwner.Memory;
            }

            /// <summary>
            /// Assigns this (recyclable) segment a new area in memory.
            /// </summary>
            /// <param name="array">An array drawn from an <see cref="ArrayPool{T}"/>.</param>
            internal void Assign(T[] array)
            {
                this.array = array;
                this.Memory = array;
            }

            /// <summary>
            /// Assigns this (recyclable) segment a new area in memory.
            /// </summary>
            /// <param name="memory">A memory block obtained from outside, that we do not own and should not recycle.</param>
            internal void AssignForeign(ReadOnlyMemory<T> memory)
            {
                this.Memory = memory;
                this.End = memory.Length;
            }

            /// <summary>
            /// Clears all fields in preparation to recycle this instance.
            /// </summary>
            internal void ResetMemory(ArrayPool<T>? arrayPool)
            {
                this.ClearReferences(this.Start, this.End - this.Start);
                this.Memory = default;
                this.Next = null;
                this.RunningIndex = 0;
                this.Start = 0;
                this.End = 0;
                if (this.array != null)
                {
                    arrayPool!.Return(this.array);
                    this.array = null;
                }
                else
                {
                    this.MemoryOwner?.Dispose();
                    this.MemoryOwner = null;
                }
            }

            /// <summary>
            /// Adds a new segment after this one.
            /// </summary>
            /// <param name="segment">The next segment in the linked list.</param>
            internal void SetNext(SequenceSegment segment)
            {
                this.Next = segment;
                segment.RunningIndex = this.RunningIndex + this.Start + this.Length;

                // Trim any slack on this segment.
                if (!this.IsForeignMemory)
                {
                    // When setting Memory, we start with index 0 instead of this.Start because
                    // the first segment has an explicit index set anyway,
                    // and we don't want to double-count it here.
                    this.Memory = this.AvailableMemory.Slice(0, this.Start + this.Length);
                }
            }

            /// <summary>
            /// Commits more elements as written in this segment.
            /// </summary>
            /// <param name="count">The number of elements written.</param>
            internal void Advance(int count)
            {
                if (!(count >= 0 && this.End + count <= this.Memory.Length))
                    throw new ArgumentOutOfRangeException(nameof(count));
                
                this.End += count;
            }

            /// <summary>
            /// Removes some elements from the start of this segment.
            /// </summary>
            /// <param name="offset">The number of elements to ignore from the start of the underlying array.</param>
            internal void AdvanceTo(int offset)
            {
                Debug.Assert(offset >= this.Start, "Trying to rewind.");
                this.ClearReferences(this.Start, offset - this.Start);
                this.Start = offset;
            }

            private void ClearReferences(int startIndex, int length)
            {
                // Clear the array to allow the objects to be GC'd.
                // Reference types need to be cleared. Value types can be structs with reference type members too, so clear everything.
                if (MayContainReferences)
                {
                    this.AvailableMemory.Span.Slice(startIndex, length).Clear();
                }
            }
        }
    }
}
