using System;
using System.Collections.Generic;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Describes snap point behavior for objects that contain and present items.
    /// </summary>
    public interface IScrollSnapPointsInfo
    {
        /// <summary>
        /// Gets or sets a value that indicates whether the horizontal snap points for the container are equidistant from each other.
        /// </summary>
        bool AreHorizontalSnapPointsRegular { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the vertical snap points for the container are equidistant from each other.
        /// </summary>
        bool AreVerticalSnapPointsRegular { get; set; }

        /// <summary>
        /// Returns the set of distances between irregular snap points for a specified orientation and alignment.
        /// </summary>
        /// <param name="orientation">The orientation for the desired snap point set.</param>
        /// <param name="snapPointsAlignment">The alignment to use when applying the snap points.</param>
        /// <returns>The read-only collection of snap point distances. Returns an empty collection when no snap points are present.</returns>
        IReadOnlyList<double> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment);

        /// <summary>
        /// Gets the distance between regular snap points for a specified orientation and alignment.
        /// </summary>
        /// <param name="orientation">The orientation for the desired snap point set.</param>
        /// <param name="snapPointsAlignment">The alignment to use when applying the snap points.</param>
        /// <param name="offset">Out parameter. The offset of the first snap point.</param>
        /// <returns>The distance between the equidistant snap points. Returns 0 when no snap points are present.</returns>
        double GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment, out double offset);

        /// <summary>
        /// Occurs when the measurements for horizontal snap points change.
        /// </summary>
        event EventHandler<RoutedEventArgs> HorizontalSnapPointsChanged;

        /// <summary>
        /// Occurs when the measurements for vertical snap points change.
        /// </summary>
        event EventHandler<RoutedEventArgs> VerticalSnapPointsChanged;
    }
}
