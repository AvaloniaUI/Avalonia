using System;
using System.ComponentModel;
using Avalonia.Metadata;

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
        /// The lowest foreground dispatcher priority
        /// </summary>
        public static readonly DispatcherPriority Default = new(0);

        internal static readonly DispatcherPriority MinimumForegroundPriority = Default;
        
        /// <summary>
        /// The job will be processed with the same priority as input.
        /// </summary>
        public static readonly DispatcherPriority Input = new(Default - 1);
        
        /// <summary>
        /// The job will be processed after other non-idle operations have completed.
        /// </summary>
        public static readonly DispatcherPriority Background = new(Input - 1);
        
        /// <summary>
        /// The job will be processed after background operations have completed.
        /// </summary>
        public static readonly DispatcherPriority ContextIdle = new(Background - 1);
        
        
        /// <summary>
        /// The job will be processed when the application is idle.
        /// </summary>
        public static readonly DispatcherPriority ApplicationIdle = new (ContextIdle - 1);
        
        /// <summary>
        /// The job will be processed when the system is idle.
        /// </summary>
        public static readonly DispatcherPriority SystemIdle = new(ApplicationIdle - 1);

        /// <summary>
        /// Minimum possible priority that's actually dispatched, default value
        /// </summary>
        internal static readonly DispatcherPriority MinimumActiveValue = new(SystemIdle);

        
        /// <summary>
        /// A dispatcher priority for jobs that shouldn't be executed yet
        /// </summary>
        public static readonly DispatcherPriority Inactive = new(MinimumActiveValue - 1);
        
        /// <summary>
        /// Minimum valid priority
        /// </summary>
        internal static readonly DispatcherPriority MinValue = new(Inactive);
        
        /// <summary>
        /// Used internally in dispatcher code
        /// </summary>
        public static readonly DispatcherPriority Invalid = new(MinimumActiveValue - 2);
        
        
        /// <summary>
        /// The job will be processed after layout and render but before input.
        /// </summary>
        public static readonly DispatcherPriority Loaded = new(Default + 1);

        /// <summary>
        /// A special priority for platforms with UI render timer or for forced full rasterization requests
        /// </summary>
        [PrivateApi]
        public static readonly DispatcherPriority UiThreadRender = new(Loaded + 1);

        /// <summary>
        /// A special priority to synchronize native control host positions, IME, etc
        /// We should probably have a better API for that, so the priority is internal
        /// </summary>
        internal static readonly DispatcherPriority AfterRender = new(UiThreadRender + 1);
        
        /// <summary>
        /// The job will be processed with the same priority as render.
        /// </summary>
        public static readonly DispatcherPriority Render = new(AfterRender + 1);
        
        /// <summary>
        /// A special platform hook for jobs to be executed before the normal render cycle
        /// </summary>
        [PrivateApi]
        public static readonly DispatcherPriority BeforeRender = new(Render + 1);
        
        /// <summary>
        /// A special priority for platforms that resize the render target in asynchronous-ish matter,
        /// should be changed into event grouping in the platform backend render
        /// </summary>
        [PrivateApi]
        public static readonly DispatcherPriority AsyncRenderTargetResize = new(BeforeRender + 1);
        
        /// <summary>
        /// The job will be processed with the same priority as data binding.
        /// </summary>
        [Obsolete("WPF compatibility"), EditorBrowsable(EditorBrowsableState.Never)] public static readonly DispatcherPriority DataBind = new(Render);
        
        /// <summary>
        /// The job will be processed with normal priority.
        /// </summary>
#pragma warning disable CS0618
        public static readonly DispatcherPriority Normal = new(DataBind + 1);
#pragma warning restore CS0618

        /// <summary>
        /// The job will be processed before other asynchronous operations.
        /// </summary>
        public static readonly DispatcherPriority Send = new(Normal + 1);

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

        public static void Validate(DispatcherPriority priority, string parameterName)
        {
            if (priority < Inactive || priority > MaxValue)
                throw new ArgumentException("Invalid DispatcherPriority value", parameterName);
        }
        
#pragma warning disable CS0618
        public override string ToString()
        {
            if (this == Invalid)
                return nameof(Invalid);
            if (this == Inactive)
                return nameof(Inactive);
            if (this == SystemIdle)
                return nameof(SystemIdle);
            if (this == ContextIdle)
                return nameof(ContextIdle);
            if (this == ApplicationIdle)
                return nameof(ApplicationIdle);
            if (this == Background)
                return nameof(Background);
            if (this == Input)
                return nameof(Input);
            if (this == Default)
                return nameof(Default);
            if (this == Loaded)
                return nameof(Loaded);
            if (this == UiThreadRender)
                return nameof(UiThreadRender);
            if (this == AfterRender)
                return nameof(AfterRender);
            if (this == Render)
                return nameof(Render);
            if (this == BeforeRender)
                return nameof(BeforeRender);
            if (this == AsyncRenderTargetResize)
                return nameof(AsyncRenderTargetResize);
            if (this == DataBind)
                return nameof(DataBind);
            if (this == Normal)
                return nameof(Normal);
            if (this == Send)
                return nameof(Send);
            return Value.ToString();
        }
#pragma warning restore CS0618
    }
}
