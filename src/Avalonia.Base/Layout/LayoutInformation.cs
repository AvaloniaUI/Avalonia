namespace Avalonia.Layout;

/// <summary>
/// Provides access to layout information of a control.
/// </summary>
public static class LayoutInformation
{
    /// <summary>
    /// Gets the available size constraint passed in the previous layout pass.
    /// </summary>
    /// <param name="control">The control.</param>
    /// <returns>Previous control measure constraint, if any.</returns>
    public static Size? GetPreviousMeasureConstraint(Layoutable control)
    {
        return control.PreviousMeasure;
    }

    /// <summary>
    /// Gets the control bounds used in the previous layout arrange pass.
    /// </summary>
    /// <param name="control">The control.</param>
    /// <returns>Previous control arrange bounds, if any.</returns>
    public static Rect? GetPreviousArrangeBounds(Layoutable control)
    {
        return control.PreviousArrange;
    }
}
