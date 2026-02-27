using Avalonia.Input;

namespace Avalonia.Controls.Chrome;

/// <summary>
/// Provides attached properties for window decoration hit-testing.
/// </summary>
public static class WindowDecorations
{
    /// <summary>
    /// Defines the <see cref="WindowDecorationsElementRole"/> attached property.
    /// Marks a visual element with a specific role for non-client hit-testing.
    /// Can be applied to any element in the visual tree, not limited to decoration children.
    /// </summary>
    public static readonly AttachedProperty<WindowDecorationsElementRole> ElementRoleProperty =
        AvaloniaProperty.RegisterAttached<Visual, WindowDecorationsElementRole>("ElementRole", typeof(WindowDecorations));

    /// <summary>
    /// Gets the <see cref="WindowDecorationsElementRole"/> for the specified element.
    /// </summary>
    public static WindowDecorationsElementRole GetElementRole(Visual element) => element.GetValue(ElementRoleProperty);

    /// <summary>
    /// Sets the <see cref="WindowDecorationsElementRole"/> for the specified element.
    /// </summary>
    public static void SetElementRole(Visual element, WindowDecorationsElementRole value) => element.SetValue(ElementRoleProperty, value);

    /// <summary>
    /// Defines the IsHitTestVisibleInChrome attached property.
    /// When true, the element participates in chrome hit-testing as an interactive chrome element
    /// (e.g., caption buttons). Internal use only.
    /// </summary>
    internal static readonly AttachedProperty<bool> IsHitTestVisibleInChromeProperty =
        AvaloniaProperty.RegisterAttached<Visual, bool>("IsHitTestVisibleInChrome", typeof(WindowDecorations));

    /// <summary>
    /// Gets whether the element is hit-test visible in chrome.
    /// </summary>
    internal static bool GetIsHitTestVisibleInChrome(Visual element) => element.GetValue(IsHitTestVisibleInChromeProperty);

    /// <summary>
    /// Sets whether the element is hit-test visible in chrome.
    /// </summary>
    internal static void SetIsHitTestVisibleInChrome(Visual element, bool value) => element.SetValue(IsHitTestVisibleInChromeProperty, value);
}
