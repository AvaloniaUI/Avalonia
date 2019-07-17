namespace Avalonia.Media
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Avalonia.Animation;
    using Avalonia.Media.Immutable;

    /// <summary>
    /// Represents the sequence of dashes and gaps that will be applied by a <see cref="Pen"/>.
    /// </summary>
    public class DashStyle : Animatable, IDashStyle, IAffectsRender, IEquatable<IDashStyle>
    {
        /// <summary>
        /// Defines the <see cref="Dashes"/> property.
        /// </summary>
        public static readonly AvaloniaProperty<IReadOnlyList<double>> DashesProperty =
            AvaloniaProperty.Register<DashStyle, IReadOnlyList<double>>(nameof(Dashes));

        /// <summary>
        /// Defines the <see cref="Offset"/> property.
        /// </summary>
        public static readonly AvaloniaProperty<double> OffsetProperty =
            AvaloniaProperty.Register<DashStyle, double>(nameof(Offset));

        private static ImmutableDashStyle s_dash;
        private static ImmutableDashStyle s_dot;
        private static ImmutableDashStyle s_dashDot;
        private static ImmutableDashStyle s_dashDotDot;

        /// <summary>
        /// Initializes a new instance of the <see cref="DashStyle"/> class.
        /// </summary>
        public DashStyle()
            : this(null, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DashStyle"/> class.
        /// </summary>
        /// <param name="dashes">The dashes collection.</param>
        /// <param name="offset">The dash sequence offset.</param>
        public DashStyle(IEnumerable<double> dashes, double offset)
        {
            Dashes = (IReadOnlyList<double>)dashes?.ToList() ?? Array.Empty<double>();
            Offset = offset;
        }

        static DashStyle()
        {
            void RaiseInvalidated(AvaloniaPropertyChangedEventArgs e)
            {
                ((DashStyle)e.Sender).Invalidated?.Invoke(e.Sender, EventArgs.Empty);
            }

            DashesProperty.Changed.Subscribe(RaiseInvalidated);
            OffsetProperty.Changed.Subscribe(RaiseInvalidated);
        }

        /// <summary>
        /// Represents a dashed <see cref="DashStyle"/>.
        /// </summary>
        public static IDashStyle Dash =>
            s_dash ?? (s_dash = new ImmutableDashStyle(new double[] { 2, 2 }, 1));

        /// <summary>
        /// Represents a dotted <see cref="DashStyle"/>.
        /// </summary>
        public static IDashStyle Dot =>
            s_dot ?? (s_dot = new ImmutableDashStyle(new double[] { 0, 2 }, 0));

        /// <summary>
        /// Represents a dashed dotted <see cref="DashStyle"/>.
        /// </summary>
        public static IDashStyle DashDot =>
            s_dashDot ?? (s_dashDot = new ImmutableDashStyle(new double[] { 2, 2, 0, 2 }, 1));

        /// <summary>
        /// Represents a dashed double dotted <see cref="DashStyle"/>.
        /// </summary>
        public static IDashStyle DashDotDot =>
            s_dashDotDot ?? (s_dashDotDot = new ImmutableDashStyle(new double[] { 2, 2, 0, 2, 0, 2 }, 1));

        /// <summary>
        /// Gets or sets the length of alternating dashes and gaps.
        /// </summary>
        public IReadOnlyList<double> Dashes
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

        /// <summary>
        /// Raised when the dash style changes.
        /// </summary>
        public event EventHandler Invalidated;

        /// <inheritdoc/>
        public override bool Equals(object obj) => DashEquals(this, obj as IDashStyle);

        /// <inheritdoc/>
        public bool Equals(IDashStyle other) => DashEquals(this, other);

        /// <inheritdoc/>
        public override int GetHashCode() => GetHashCode(this);

        /// <summary>
        /// Returns an immutable clone of the <see cref="DashStyle"/>.
        /// </summary>
        /// <returns></returns>
        public ImmutableDashStyle ToImmutable() => new ImmutableDashStyle(Dashes, Offset);

        internal static bool DashEquals(IDashStyle a, IDashStyle b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }
            else if ((a is null && !(b is null)) || (b is null && !(a is null)))
            {
                return false;
            }

            if (a.Offset != b.Offset)
            {
                return false;
            }

            return SequenceEqual(a.Dashes, b.Dashes);
        }

        internal static int GetHashCode(IDashStyle style)
        {
            var hashCode = 717868523;
            hashCode = hashCode * -1521134295 + style.Offset.GetHashCode();

            if (style.Dashes != null)
            {
                foreach (var i in style.Dashes)
                {
                    hashCode = hashCode * -1521134295 + i.GetHashCode();
                }
            }

            return hashCode;
        }

        private static bool SequenceEqual(IReadOnlyList<double> left, IReadOnlyList<double> right)
        {
            if (left == right)
            {
                return true;
            }

            if (left == null || right == null || left.Count != right.Count)
            {
                return false;
            }

            for (var c = 0; c < left.Count; c++)
            {
                if (left[c] != right[c])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
