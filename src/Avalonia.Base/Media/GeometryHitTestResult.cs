namespace Avalonia.Media
{
    /// <summary>
    /// Returns the results of a hit test that uses a <see cref="Geometry"/> as a hit test parameter.
    /// </summary>
    /// <param name="VisualHit">Gets the <see cref="Visual"/> that is returned from a hit test result.</param>
    /// <param name="IntersectionResult">Gets the <see cref="IntersectionResult"/> value of the hit test.</param>
    public record class GeometryHitTestResult(Visual? VisualHit, IntersectionResult IntersectionResult);
}
