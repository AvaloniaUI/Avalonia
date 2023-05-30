using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Animation;
using Avalonia.Collections;
using Avalonia.Media.Immutable;
using Avalonia.Reactive;

#nullable enable

namespace Avalonia.Media
{
    /// <summary>
    /// Represents the sequence of dashes and gaps that will be applied by a <see cref="Pen"/>.
    /// </summary>
    public sealed class DashStyle : Animatable, IDashStyle
    {
        /// <summary>
        /// Defines the <see cref="Dashes"/> property.
        /// </summary>
        public static readonly StyledProperty<AvaloniaList<double>?> DashesProperty =
            AvaloniaProperty.Register<DashStyle, AvaloniaList<double>?>(nameof(Dashes));

        /// <summary>
        /// Defines the <see cref="Offset"/> property.
        /// </summary>
        public static readonly StyledProperty<double> OffsetProperty =
            AvaloniaProperty.Register<DashStyle, double>(nameof(Offset));

        private static ImmutableDashStyle? s_dash;
        private static ImmutableDashStyle? s_dot;
        private static ImmutableDashStyle? s_dashDot;
        private static ImmutableDashStyle? s_dashDotDot;

        /// <summary>
        /// Initializes a new instance of the <see cref="DashStyle"/> class.
        /// </summary>
        public DashStyle()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DashStyle"/> class.
        /// </summary>
        /// <param name="dashes">The dashes collection.</param>
        /// <param name="offset">The dash sequence offset.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1012", 
            Justification = "Collection properties shouldn't be set with SetCurrentValue.")]
        public DashStyle(IEnumerable<double>? dashes, double offset)
        {
            Dashes = (dashes as AvaloniaList<double>) ?? new AvaloniaList<double>(dashes ?? Array.Empty<double>());
            Offset = offset;
        }

        static DashStyle()
        {
            var invalidateObserver = new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(
                static e => ((DashStyle)e.Sender).Invalidated?.Invoke(e.Sender, EventArgs.Empty));

            DashesProperty.Changed.Subscribe(invalidateObserver);
            OffsetProperty.Changed.Subscribe(invalidateObserver);
        }

        /// <summary>
        /// Represents a dashed <see cref="DashStyle"/>.
        /// </summary>
        public static IDashStyle Dash => s_dash ??= new ImmutableDashStyle(new double[] { 2, 2 }, 1);

        /// <summary>
        /// Represents a dotted <see cref="DashStyle"/>.
        /// </summary>
        public static IDashStyle Dot => s_dot ??= new ImmutableDashStyle(new double[] { 0, 2 }, 0);

        /// <summary>
        /// Represents a dashed dotted <see cref="DashStyle"/>.
        /// </summary>
        public static IDashStyle DashDot => s_dashDot ??= new ImmutableDashStyle(new double[] { 2, 2, 0, 2 }, 1);

        /// <summary>
        /// Represents a dashed double dotted <see cref="DashStyle"/>.
        /// </summary>
        public static IDashStyle DashDotDot => s_dashDotDot ??= new ImmutableDashStyle(new double[] { 2, 2, 0, 2, 0, 2 }, 1);

        /// <summary>
        /// Gets or sets the length of alternating dashes and gaps.
        /// </summary>
        public AvaloniaList<double>? Dashes
        {
            get => GetValue(DashesProperty);
            set => SetValue(DashesProperty, value);
        }

        /// <summary>
        /// Gets or sets how far in the dash sequence the stroke will start.
        /// </summary>
        public double Offset
        {
            get => GetValue(OffsetProperty);
            set => SetValue(OffsetProperty, value);
        }

        IReadOnlyList<double>? IDashStyle.Dashes => Dashes;

        /// <summary>
        /// Raised when the dash style changes.
        /// </summary>
        internal event EventHandler? Invalidated;

        /// <summary>
        /// Returns an immutable clone of the <see cref="DashStyle"/>.
        /// </summary>
        /// <returns></returns>
        public ImmutableDashStyle ToImmutable() => new ImmutableDashStyle(Dashes, Offset);

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == DashesProperty)
            {
                var (oldValue, newValue) = change.GetOldAndNewValue<AvaloniaList<double>>();

                if (oldValue is object)
                {
                    oldValue.CollectionChanged -= DashesChanged;
                }

                if (newValue is object)
                {
                    newValue.CollectionChanged += DashesChanged;
                }
            }
        }

        private void DashesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Invalidated?.Invoke(this, e);
        }
    }
}
