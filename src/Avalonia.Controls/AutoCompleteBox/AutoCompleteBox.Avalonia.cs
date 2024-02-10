// This file contains improvements to AutoCompleteBox Avalonia respect the upstream Microsoft AutoCompleteBox
// These improvements are added to a separate file to try not to interfere when you eventually need to sync Avalonia AutoCompleteBox with upstream.
namespace Avalonia.Controls;

partial class AutoCompleteBox
{
    /// <summary>
    /// Defines the <see cref="InnerLeftContent"/> property
    /// </summary>
    public static readonly StyledProperty<object> InnerLeftContentProperty =
        TextBox.InnerLeftContentProperty.AddOwner<AutoCompleteBox>();

    /// <summary>
    /// Defines the <see cref="InnerRightContent"/> property
    /// </summary>
    public static readonly StyledProperty<object> InnerRightContentProperty =
        TextBox.InnerRightContentProperty.AddOwner<AutoCompleteBox>();

    /// <summary>
    /// Gets or sets custom content that is positioned on the left side of the text layout box
    /// </summary>
    public object InnerLeftContent
    {
        get => GetValue(InnerLeftContentProperty);
        set => SetValue(InnerLeftContentProperty, value);
    }

    /// <summary>
    /// Gets or sets custom content that is positioned on the right side of the text layout box
    /// </summary>
    public object InnerRightContent
    {
        get => GetValue(InnerRightContentProperty);
        set => SetValue(InnerRightContentProperty, value);
    }
}
