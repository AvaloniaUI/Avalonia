using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Diagnostics;
using Avalonia.Metadata;
using Avalonia.Utilities;

namespace Avalonia.Platform
{
    /// <summary>
    /// Describes the orientation of a screen.
    /// </summary>
    public enum ScreenOrientation
    {
        /// <summary>
        /// No screen orientation is specified.
        /// </summary>
        None,

        /// <summary>
        /// Specifies that the monitor is oriented in landscape mode where the width of the screen viewing area is greater than the height.
        /// </summary>
        Landscape = 1,

        /// <summary>
        /// Specifies that the monitor rotated 90 degrees in the clockwise direction to orient the screen in portrait mode
        /// where the height of the screen viewing area is greater than the width.
        /// </summary>
        Portrait = 2,

        /// <summary>
        /// Specifies that the monitor rotated another 90 degrees in the clockwise direction (to equal 180 degrees) to orient the screen in landscape mode
        /// where the width of the screen viewing area is greater than the height.
        /// This landscape mode is flipped 180 degrees from the Landscape mode.
        /// </summary>
        LandscapeFlipped = 4,

        /// <summary>
        /// Specifies that the monitor rotated another 90 degrees in the clockwise direction (to equal 270 degrees) to orient the screen in portrait mode
        /// where the height of the screen viewing area is greater than the width. This portrait mode is flipped 180 degrees from the Portrait mode.
        /// </summary>
        PortraitFlipped = 8
    }

    /// <summary>
    /// Represents a single display screen.
    /// </summary>
    public class Screen : IEquatable<Screen>
    {
        /// <summary>
        /// Gets the device name associated with a display.
        /// </summary>
        public string? DisplayName { get; protected set; }

        /// <summary>
        /// Gets the current orientation of a screen.
        /// </summary>
        public ScreenOrientation CurrentOrientation { get; protected set; } 

        /// <summary>
        /// Gets the scaling factor applied to the screen by the operating system.
        /// </summary>
        /// <remarks>
        /// Multiply this value by 100 to get a percentage.
        /// Both X and Y scaling factors are assumed uniform.
        /// </remarks>
        public double Scaling { get; protected set; } = 1;

        /// <inheritdoc cref="Scaling"/>
        [Obsolete("Use the Scaling property instead.", true), EditorBrowsable(EditorBrowsableState.Never)]
        public double PixelDensity => Scaling;

        /// <summary>
        /// Gets the overall pixel-size and position of the screen.
        /// </summary>
        /// <remarks>
        /// This generally is the raw pixel counts in both the X and Y direction.
        /// </remarks>
        public PixelRect Bounds { get; protected set; }

        /// <summary>
        /// Gets the actual working-area pixel-size of the screen.
        /// </summary>
        /// <remarks>
        /// This area may be smaller than <see href="Bounds"/> to account for notches and
        /// other block-out areas such as taskbars etc.
        /// </remarks>
        public PixelRect WorkingArea { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether the screen is the primary one.
        /// </summary>
        public bool IsPrimary { get; protected set; }

        /// <inheritdoc cref="IsPrimary"/>
        [Obsolete("Use the IsPrimary property instead.", true), EditorBrowsable(EditorBrowsableState.Never)]
        public bool Primary => IsPrimary;

        /// <summary>
        /// Initializes a new instance of the <see cref="Screen"/> class.
        /// </summary>
        /// <param name="scaling">The scaling factor applied to the screen by the operating system.</param>
        /// <param name="bounds">The overall pixel-size of the screen.</param>
        /// <param name="workingArea">The actual working-area pixel-size of the screen.</param>
        /// <param name="isPrimary">Whether the screen is the primary one.</param>
        [Unstable(ObsoletionMessages.MayBeRemovedInAvalonia12)]
        public Screen(double scaling, PixelRect bounds, PixelRect workingArea, bool isPrimary)
        {
            Scaling = scaling;
            Bounds = bounds;
            WorkingArea = workingArea;
            IsPrimary = isPrimary;
        }

        private protected Screen() { }

        /// <summary>
        /// Tries to get the platform handle for the Screen.
        /// </summary>
        /// <returns>
        /// An <see cref="IPlatformHandle"/> describing the screen handle, or null if the handle
        /// could not be retrieved.
        /// </returns>
        public virtual IPlatformHandle? TryGetPlatformHandle() => null;

        // TODO12: make abstract
        /// <inheritdoc />
        public override int GetHashCode()
            => RuntimeHelpers.GetHashCode(this);

        /// <inheritdoc />
        public override bool Equals(object? obj)
            => obj is Screen other && Equals(other);

        // TODO12: make abstract
        /// <inheritdoc/>
        public virtual bool Equals(Screen? other)
            => ReferenceEquals(this, other);

        public static bool operator ==(Screen? left, Screen? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Screen? left, Screen? right)
        {
            return !Equals(left, right);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = StringBuilderCache.Acquire();
            sb.Append("Screen");
            sb.Append(" { ");

            // Only printing properties that are supposed to be immutable:
            sb.AppendFormat("{0} = {1}", nameof(DisplayName), DisplayName);
            if (TryGetPlatformHandle() is { } platformHandle)
            {
                sb.AppendFormat(", {0}: {1}", platformHandle.HandleDescriptor, platformHandle.Handle);
            }

            sb.Append(" } ");
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        /// <summary>
        /// When screen is removed, we should at least empty all the properties. 
        /// </summary>
        internal void OnRemoved()
        {
            DisplayName = null;
            Bounds = WorkingArea = default;
            Scaling = default;
            CurrentOrientation = default;
        }
    }
}
