using Avalonia.Metadata;

namespace Avalonia.Input.TextInput
{
    /// <summary>
    /// A well-known paragraph or character style an <see cref="ITextNavigation"/> can report via
    /// <see cref="TextAttribute.StyleId"/> - most importantly the heading levels, which screen readers use
    /// for "navigate by heading". Mirrors the UIA StyleId vocabulary; the platform accessibility layer maps
    /// it to its protocol form (UIA <c>StyleId_*</c>).
    /// </summary>
    [Unstable]
    public enum TextStyleId
    {
        /// <summary>A custom or unspecified style.</summary>
        Custom = 0,
        Heading1,
        Heading2,
        Heading3,
        Heading4,
        Heading5,
        Heading6,
        Heading7,
        Heading8,
        Heading9,
        Title,
        Subtitle,
        Normal,
        Emphasis,
        Quote,
        BulletedList,
        NumberedList,
    }
}
