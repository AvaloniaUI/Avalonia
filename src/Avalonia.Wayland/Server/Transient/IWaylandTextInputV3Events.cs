using Avalonia.SourceGenerator;
using Avalonia.Threading;
using Avalonia.Wayland.Server;

namespace Avalonia.Wayland.Server.Transient;

/// <summary>
/// Worker → UI callback interface for <c>zwp_text_input_v3</c>.
/// All indices are UTF-16 char positions (i.e. .NET <see cref="string"/> indices),
/// not UTF-8 bytes — the worker performs the conversion before invoking the sink.
/// </summary>
/// <remarks>
/// The worker invokes this method on the worker thread; the generated
/// <c>WaylandTextInputV3EventsProxy</c> marshals each call to the UI thread.
/// A single call corresponds to one wayland <c>done</c> batch — the four
/// operations (clear-preedit, delete-surrounding, commit, new-preedit) are
/// applied atomically by the UI broker so no other UI work can interleave
/// between them.
/// </remarks>
[GenerateCrossThreadProxy(
    typeof(DispatcherPriority),
    "default",
    GeneratedClassName = "WaylandTextInputV3EventsProxy")]
internal interface IWaylandTextInputV3Events
{
    /// <summary>
    /// Apply one IME update batch. Operations are applied in protocol
    /// order: clear current preedit, delete surrounding, insert commit,
    /// install new preedit.
    /// </summary>
    /// <param name="sessionToken">
    /// The session token last received via <c>IWSurface.SetTextInputActive</c>;
    /// the UI broker drops batches whose token doesn't match its current
    /// client epoch.
    /// </param>
    /// <param name="deleteBeforeChars">UTF-16 chars to delete before cursor (0 = no delete).</param>
    /// <param name="deleteAfterChars">UTF-16 chars to delete after cursor (0 = no delete).</param>
    /// <param name="commitText">Text to commit; null/empty = no commit.</param>
    /// <param name="preeditText">New preedit text; null/empty = clear preedit.</param>
    /// <param name="preeditCursorBeginChar">Preedit cursor start (UTF-16 chars; -1 = hidden).</param>
    /// <param name="preeditCursorEndChar">Preedit cursor end (UTF-16 chars; -1 = hidden).</param>
    void OnImeUpdate(
        int sessionToken,
        int deleteBeforeChars,
        int deleteAfterChars,
        string? commitText,
        string? preeditText,
        int preeditCursorBeginChar,
        int preeditCursorEndChar);
}
