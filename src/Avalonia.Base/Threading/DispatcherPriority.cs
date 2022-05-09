using System;

namespace Avalonia.Threading
{
    /// <summary>
    /// Defines the priorities with which jobs can be invoked on a <see cref="Dispatcher"/>.
    /// </summary>
    public readonly struct DispatcherPriority : IEquatable<DispatcherPriority>, IComparable<DispatcherPriority>
    {
        /// <summary>
        /// The integer value of the priority
        /// </summary>
        public int Value { get; }

        private DispatcherPriority(int value)
        {
            Value = value;
        }

        /// <summary>
        /// Minimum possible priority
        /// </summary>
        public static readonly DispatcherPriority MinValue = new(0);

        /// <summary>
        /// The job will be processed when the system is idle.
        /// </summary>
        [Obsolete("WPF compatibility")] public static readonly DispatcherPriority SystemIdle = MinValue;

        /// <summary>
        /// The job will be processed when the application is idle.
        /// </summary>
        [Obsolete("WPF compatibility")] public static readonly DispatcherPriority ApplicationIdle = MinValue;

        /// <summary>
        /// The job will be processed after background operations have completed.
        /// </summary>
        [Obsolete("WPF compatibility")] public static readonly DispatcherPriority ContextIdle = MinValue;

        /// <summary>
        /// The job will be processed with normal priority.
        /// </summary>
        public static readonly DispatcherPriority Normal = MinValue;

        /// <summary>
        /// The job will be processed after other non-idle operations have completed.
        /// </summary>
        public static readonly DispatcherPriority Background = new(1);

        /// <summary>
        /// The job will be processed with the same priority as input.
        /// </summary>
        public static readonly DispatcherPriority Input = new(2);

        /// <summary>
        /// The job will be processed after layout and render but before input.
        /// </summary>
        public static readonly DispatcherPriority Loaded = new(3);
        
        /// <summary>
        /// The job will be processed with the same priority as render.
        /// </summary>
        public static readonly DispatcherPriority Render = new(5);

        /// <summary>
        /// The job will be processed with the same priority as composition batch commit.
        /// </summary>
        public static readonly DispatcherPriority CompositionBatch = new(6);
        
        /// <summary>
        /// The job will be processed with the same priority as composition updates.
        /// </summary>
        public static readonly DispatcherPriority Composition = new(7);
        
        /// <summary>
        /// The job will be processed with the same priority as render.
        /// </summary>
        public static readonly DispatcherPriority Layout = new(8);

        /// <summary>
        /// The job will be processed with the same priority as data binding.
        /// </summary>
        [Obsolete("WPF compatibility")] public static readonly DispatcherPriority DataBind = MinValue;

        /// <summary>
        /// The job will be processed before other asynchronous operations.
        /// </summary>
        public static readonly DispatcherPriority Send = new(9);

        /// <summary>
        /// Maximum possible priority
        /// </summary>
        public static readonly DispatcherPriority MaxValue = Send;

        // Note: unlike ctor this one is validating
        public static DispatcherPriority FromValue(int value)
        {
            if (value < MinValue.Value || value > MaxValue.Value)
                throw new ArgumentOutOfRangeException(nameof(value));
            return new DispatcherPriority(value);
        }

        public static implicit operator int(DispatcherPriority priority) => priority.Value;

        public static implicit operator DispatcherPriority(int value) => FromValue(value);

        /// <inheritdoc />
        public bool Equals(DispatcherPriority other) => Value == other.Value;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is DispatcherPriority other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(DispatcherPriority left, DispatcherPriority right) => left.Value == right.Value;

        public static bool operator !=(DispatcherPriority left, DispatcherPriority right) => left.Value != right.Value;

        public static bool operator <(DispatcherPriority left, DispatcherPriority right) => left.Value < right.Value;

        public static bool operator >(DispatcherPriority left, DispatcherPriority right) => left.Value > right.Value;

        public static bool operator <=(DispatcherPriority left, DispatcherPriority right) => left.Value <= right.Value;

        public static bool operator >=(DispatcherPriority left, DispatcherPriority right) => left.Value >= right.Value;

        /// <inheritdoc />
        public int CompareTo(DispatcherPriority other) => Value.CompareTo(other.Value);
    }
}