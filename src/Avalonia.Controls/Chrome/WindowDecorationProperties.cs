using Avalonia.Input;

namespace Avalonia.Controls.Chrome;

/// <summary>
/// Provides attached properties for window decoration hit-testing.
/// </summary>
public static class WindowDecorationProperties
{
    /// <summary>
    /// Defines the <see cref="WindowDecorationsElementRole"/> attached property.
    /// Marks a visual element with a specific role for non-client hit-testing.
    /// Can be applied to any element in the visual tree, not limited to decoration children.
    /// </summary>
    /// <remarks>Setting an element role only has an effect if <see cref="Window.ExtendClientAreaToDecorationsHint"/> is true.</remarks>
    public static readonly AttachedProperty<WindowDecorationsElementRole> ElementRoleProperty =
        AvaloniaProperty.RegisterAttached<Visual, WindowDecorationsElementRole>("ElementRole", typeof(WindowDecorationProperties));

    /// <summary>
    /// Gets the <see cref="WindowDecorationsElementRole"/> for the specified element.
    /// </summary>
    public static WindowDecorationsElementRole GetElementRole(Visual element) => element.GetValue(ElementRoleProperty);

    /// <summary>
    /// Sets the <see cref="WindowDecorationsElementRole"/> for the specified element.
    /// </summary>
    /// <remarks>Setting an element role only has an effect if <see cref="Window.ExtendClientAreaToDecorationsHint"/> is true.</remarks>
    public static void SetElementRole(Visual element, WindowDecorationsElementRole value) => element.SetValue(ElementRoleProperty, value);
}
