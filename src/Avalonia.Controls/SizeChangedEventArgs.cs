using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data specific to a SizeChanged event.
    /// </summary>
    public class SizeChangedEventArgs : RoutedEventArgs
    {
        public SizeChangedEventArgs(RoutedEvent? routedEvent)
            : base (routedEvent)
        {
        }

        public SizeChangedEventArgs(RoutedEvent? routedEvent, IInteractive? source)
            : base(routedEvent, source)
        {
        }

        public SizeChangedEventArgs(
            RoutedEvent? routedEvent,
            IInteractive? source,
            Size newSize,
            Size previousSize)
            : base(routedEvent, source)
        {
            NewSize = newSize;
            PreviousSize = previousSize;
            HeightChanged = newSize.Height != previousSize.Height;
            WidthChanged = newSize.Width != previousSize.Width;
        }

        /// <summary>
        /// Gets a value indicating whether the height of the new size is different
        /// than the previous size height.
        /// </summary>
        public bool HeightChanged { get; init; }

        /// <summary>
        /// Gets the new size (or bounds) of the object.
        /// </summary>
        public Size NewSize { get; init; }

        /// <summary>
        /// Gets the previous size (or bounds) of the object.
        /// </summary>
        public Size PreviousSize { get; init; }

        /// <summary>
        /// Gets a value indicating whether the width of the new size is different
        /// than the previous size width.
        /// </summary>
        public bool WidthChanged { get; init; }
    }
}
