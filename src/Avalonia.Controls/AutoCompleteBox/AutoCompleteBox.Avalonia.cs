// This file contains improvements to AutoCompleteBox Avalonia respect the upstream Microsoft AutoCompleteBox
// These improvements are added to a separate file to try not to interfere when you eventually need to sync Avalonia AutoCompleteBox with upstream.
namespace Avalonia.Controls;

partial class AutoCompleteBox
{
    /// <summary>
    /// Defines the <see cref="MaxLength"/> property
    /// </summary>
    public static readonly StyledProperty<int> MaxLengthProperty =
        TextBox.MaxLengthProperty.AddOwner<AutoCompleteBox>();

    /// <summary>
    /// Gets or sets the maximum number of characters that the <see cref="AutoCompleteBox"/> can accept.
    /// This constraint only applies for manually entered (user-inputted) text.
    /// </summary>
    public int MaxLength
    {
        get => GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }
}
