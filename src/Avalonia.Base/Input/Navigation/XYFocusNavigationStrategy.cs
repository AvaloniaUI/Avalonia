namespace Avalonia.Input;

/// <summary>
/// Specifies the disambiguation strategy used for navigating between multiple candidate targets using
/// <see cref="XYFocus.DownNavigationStrategyProperty"/>, <see cref="XYFocus.LeftNavigationStrategyProperty"/>,
/// <see cref="XYFocus.RightNavigationStrategyProperty"/>, and <see cref="XYFocus.UpNavigationStrategyProperty"/>.
/// </summary>
public enum XYFocusNavigationStrategy
{
    /// <summary>
    /// Indicates that navigation strategy is inherited from the element's ancestors. If all ancestors have a value of Auto, the fallback strategy is Projection.
    /// </summary>
    Auto,
    
    /// <summary>
    /// Indicates that focus moves to the first element encountered when projecting the edge of the currently focused element in the direction of navigation.
    /// </summary>
    Projection = 1,
    
    /// <summary>
    /// Indicates that focus moves to the element closest to the axis of the navigation direction.
    /// </summary>
    /// <remarks>
    /// The edge of the bounding rect corresponding to the navigation direction is extended and projected to identify candidate targets. The first element encountered is identified as the target. In the case of multiple candidates, the closest element is identified as the target. If there are still multiple candidates, the topmost/leftmost element is identified as the candidate.
    /// </remarks>
    NavigationDirectionDistance = 2,
    
    /// <summary>
    /// Indicates that focus moves to the closest element based on the shortest 2D distance (Manhattan metric).
    /// </summary>
    /// <remarks>
    /// This distance is calculated by adding the primary distance and the secondary distance of each potential candidate. In the case of a tie:
    /// - The first element to the left is selected if the navigation direction is up or down
    /// - The first element to the top is selected if the navigation direction is left or right
    /// Here we show how focus moves from A to B based on rectilinear distance.
    /// - Distance (A, B, Down) = 10 + 0 = 10
    /// - Distance (A, C, Down) = 0 + 30 = 30
    /// - Distance (A, D, Down) 30 + 0 = 30
    /// </remarks>
    RectilinearDistance = 3
}
