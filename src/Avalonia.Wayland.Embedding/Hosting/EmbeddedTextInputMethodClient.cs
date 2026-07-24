using Avalonia.Input.TextInput;

namespace Avalonia.Wayland.Embedding.Hosting;

/// <summary>
/// Bridges Avalonia's IME framework to the embedded toolkit's <c>zwp_text_input_v3</c>. The host control offers
/// this as its <see cref="TextInputMethodClient"/> (via TextInputMethodClientRequestedEvent), so the outer OS IME
/// composes into the embedded content: composition (<see cref="SetPreeditText(string?,int?)"/>) is forwarded to
/// the toolkit as <c>preedit_string</c>, and the toolkit's caret rectangle is surfaced back as
/// <see cref="CursorRectangle"/> so the OS candidate window positions over the embedded caret. Final committed
/// text still arrives via the control's TextInput event → <c>commit_string</c> (the existing path).
/// </summary>
internal sealed class EmbeddedTextInputMethodClient : TextInputMethodClient
{
    private readonly WaylandSubcompositorControlHost _host;
    private Rect _cursorRectangle;

    public EmbeddedTextInputMethodClient(WaylandSubcompositorControlHost host) => _host = host;

    public override Visual TextViewVisual => _host;
    public override bool SupportsPreedit => true;
    // We don't surface the toolkit's surrounding text to the OS IME yet (zwp_text_input_v3.set_surrounding_text
    // is accepted but not bridged) — composition + caret positioning are the high-value path. See deferred items.
    public override bool SupportsSurroundingText => false;
    public override string SurroundingText => "";
    public override Rect CursorRectangle => _cursorRectangle;
    public override TextSelection Selection { get; set; }

    public override void SetPreeditText(string? preeditText) => _host.ForwardPreedit(preeditText, null);
    public override void SetPreeditText(string? preeditText, int? cursorPos) => _host.ForwardPreedit(preeditText, cursorPos);

    /// <summary>Reverse direction: the toolkit reported its caret rectangle (zwp_text_input_v3.set_cursor_rectangle,
    /// mapped into host coordinates) — surface it to the OS IME so its candidate window tracks the embedded caret.</summary>
    internal void UpdateCursorRectangle(Rect rect)
    {
        if (_cursorRectangle == rect)
            return;
        _cursorRectangle = rect;
        RaiseCursorRectangleChanged();
    }
}
