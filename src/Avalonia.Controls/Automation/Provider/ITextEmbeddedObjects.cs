using System.Collections.Generic;
using Avalonia.Automation.Peers;
using Avalonia.Input.TextInput;
using Avalonia.Metadata;

namespace Avalonia.Automation.Provider
{
    /// <summary>
    /// An optional add-on to <see cref="IAccessibleText"/> for text that contains embedded automation
    /// elements - hyperlinks, images, tables. A navigation that implements it lets the text providers
    /// expose those objects as children of a text range (UIA <c>GetChildren</c>) and map between an
    /// element and its text extent (<c>RangeFromChild</c> / <c>GetEnclosingElement</c>).
    /// </summary>
    /// <remarks>
    /// Navigations over uniform text (e.g. a plain TextBox) simply do not implement this interface, and
    /// the providers report no embedded children for them.
    /// </remarks>
    [Unstable]
    public interface ITextEmbeddedObjects
    {
        /// <summary>
        /// The embedded automation elements whose extent overlaps <paramref name="range"/>, in document
        /// order; empty when the range contains none.
        /// </summary>
        IReadOnlyList<AutomationPeer> GetEmbeddedObjects(ITextRange range);

        /// <summary>
        /// The text extent of an embedded element previously surfaced by
        /// <see cref="GetEmbeddedObjects"/>, or null when <paramref name="child"/> is not one of them.
        /// </summary>
        ITextRange? GetEmbeddedObjectRange(AutomationPeer child);

        /// <summary>
        /// The innermost embedded element that fully encloses <paramref name="range"/>, or null when the
        /// range is not contained in one - the text container is then the enclosing element.
        /// </summary>
        AutomationPeer? GetEnclosingElement(ITextRange range);
    }
}
