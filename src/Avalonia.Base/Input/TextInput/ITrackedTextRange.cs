using System;
using Avalonia.Metadata;

namespace Avalonia.Input.TextInput
{
    /// <summary>
    /// A range whose endpoints follow edits, produced by <see cref="ITextNavigation.TrackRange"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ITextRange.Start"/> and <see cref="ITextRange.End"/> are always pointers for the current
    /// document. Each endpoint moves with the content around it, following its gravity:
    /// </para>
    /// <list type="bullet">
    /// <item>Content inserted before an endpoint shifts it; content inserted after it leaves it.</item>
    /// <item>Content inserted exactly at an endpoint lands before a forward endpoint and after a backward one.</item>
    /// <item>
    /// Replaced content keeps an endpoint at its start before the new content and one at its end after
    /// it, so a range that covered the old content covers the replacement.
    /// </item>
    /// <item>
    /// An endpoint strictly inside replaced or removed content collapses to the change, after any new
    /// content when forward and before it when backward.
    /// </item>
    /// <item>The range never inverts: should the start pass the end, the end is moved to the start.</item>
    /// </list>
    /// <para>
    /// The navigation updates the range before it raises <see cref="ITextNavigation.TextChanged"/>, so a
    /// handler sees it current. A tracked range costs work on every edit until it is disposed; dispose it
    /// as soon as the position no longer matters. A navigation may also stop tracking a range nothing
    /// references any more, so a consumer that cannot tell when it is done, such as an automation text
    /// range a client holds, may drop the range instead. After disposal the endpoints keep their last
    /// values and stop moving.
    /// </para>
    /// </remarks>
    [Unstable]
    public interface ITrackedTextRange : ITextRange, IDisposable
    {
    }
}
