using System;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Wayland.Server;
using Avalonia.Wayland.Server.Persistent;
using Avalonia.Wayland.Server.Transient;

namespace Avalonia.Wayland;

partial class WindowImpl
{
    /// <summary>
    /// UI-thread broker for text input. Combines two responsibilities so they
    /// stay coherent on compositors with or without zwp_text_input_v3:
    /// 1. Compose-key gating — flips <c>WSurface.HasTextInputClient</c> on the
    ///    Wayland thread so XKB-compose key sequences are processed if a text
    ///    input client is attached.
    /// 2. zwp_text_input_v3 IME — when the compositor exposes the manager,
    ///    drives the worker-side <see cref="WaylandTextInputV3"/> facade and
    ///    surfaces its callbacks back to the active <see cref="TextInputMethodClient"/>.
    /// </summary>
    /// <remarks>
    /// All worker→UI callbacks (<see cref="IWaylandTextInputV3Events"/>) are
    /// marshalled by the generated <c>WaylandTextInputV3EventsProxy</c> the
    /// broker registers with the surface, so the methods below run on the UI
    /// thread despite being invoked by the worker.
    /// </remarks>
    private sealed class WaylandTextInputMethod
        : ITextInputMethodImpl, IWaylandTextInputV3Events
    {
        private readonly WindowImpl _owner;
        private TextInputMethodClient? _client;
        private bool _hasPreedit;
        private WaylandTextInputV3EventsProxy? _sinkProxy;

        // Each callback the worker emits carries the session token most
        // recently set via SetTextInputActive. SetClient bumps _clientEpoch
        // and propagates it to the worker; worker→UI callbacks emitted under
        // a previous client carry the old token and are dropped by the guard
        // at the top of every IWaylandTextInputV3Events method.
        private int _clientEpoch;

        // Tracks the surface proxy we registered the sink against. After
        // hide/show the surface is recreated; we need to re-register the
        // sink and re-apply the current text-input state.
        private WXdgTopLevelProxy? _registeredAgainstSurface;

        public WaylandTextInputMethod(WindowImpl owner)
        {
            _owner = owner;
        }

        // ---- ITextInputMethodImpl ----

        public void SetClient(TextInputMethodClient? client)
        {
            var oldClient = _client;
            if (ReferenceEquals(oldClient, client))
                return;

            UnsubscribeClient();

            // Bump epoch BEFORE swapping _client. The worker stamps every
            // subsequent IME callback with the token we send below; older
            // callbacks (for the previous client) carry the previous token
            // and are dropped at the top of each OnImeXxx handler.
            _clientEpoch++;

            // Abort any in-flight composition on the old client (if any).
            if (oldClient != null && _hasPreedit)
            {
                try { oldClient.SetPreeditText(null, null); } catch { }
            }
            _hasPreedit = false;

            _client = client;
            var hasClient = client != null;
            var supportsPreedit = client?.SupportsPreedit ?? false;
            var supportsSurrounding = client?.SupportsSurroundingText ?? false;

            ApplyTextInputStateToSurface(hasClient, supportsPreedit, supportsSurrounding);

            if (client != null)
            {
                client.SurroundingTextChanged += OnClientSurroundingTextChanged;
                client.SelectionChanged += OnClientSelectionChanged;
                PushSurrounding();
            }
        }

        /// <summary>
        /// Called by <see cref="WindowImpl.Sink"/> after a new worker surface
        /// is created (Show following Hide). Re-registers the sink on the
        /// freshly created worker <c>WSurface</c> and re-applies the current
        /// text-input state so the IME keeps working across surface
        /// recreation.
        /// </summary>
        public void OnSurfaceCreated()
        {
            if (_owner._surfaceProxy is null)
                return;
            // Force re-registration on the new surface even if we already
            // have a sink proxy — the worker-side sink lives on WSurface and
            // is reset when the surface is recreated.
            _registeredAgainstSurface = null;

            var hasClient = _client != null;
            var supportsPreedit = _client?.SupportsPreedit ?? false;
            var supportsSurrounding = _client?.SupportsSurroundingText ?? false;
            ApplyTextInputStateToSurface(hasClient, supportsPreedit, supportsSurrounding);

            if (hasClient)
                PushSurrounding();
        }

        private void ApplyTextInputStateToSurface(bool hasClient, bool supportsPreedit, bool supportsSurrounding)
        {
            var proxy = _owner._surfaceProxy;
            if (proxy is null)
                return;

            if (!ReferenceEquals(_registeredAgainstSurface, proxy))
            {
                _sinkProxy ??= new WaylandTextInputV3EventsProxy(this, WaylandMarshallers.UIThread);
                proxy.RegisterTextInputSink(_sinkProxy);
                _registeredAgainstSurface = proxy;
            }

            proxy.SetTextInputActive(hasClient, supportsPreedit, supportsSurrounding, _clientEpoch);
        }

        public void SetCursorRect(Rect rect) =>
            _owner._surfaceProxy?.SetTextInputCursorRect(rect);

        public void SetOptions(TextInputOptions options) =>
            _owner._surfaceProxy?.SetTextInputOptions(options);

        public void Reset() =>
            _owner._surfaceProxy?.ResetTextInput();

        // ---- IWaylandTextInputV3Events (called on UI thread by the proxy) ----

        void IWaylandTextInputV3Events.OnImeUpdate(
            int sessionToken,
            int deleteBeforeChars, int deleteAfterChars,
            string? commitText,
            string? preeditText, int preeditCursorBeginChar, int preeditCursorEndChar)
        {
            // The session token rejects in-flight callbacks queued on the UI
            // dispatcher before SetClient (or surface recreation) bumped the
            // epoch — those would otherwise apply to the wrong client.
            if (sessionToken != _clientEpoch || _client is not { } client)
                return;
            if (_owner._inputRoot is not { } inputRoot)
                return;

            var input = _owner.Input;

            // Per spec, in this exact order:
            //   1. clear current preedit (implicit before commit/delete);
            //   2. apply delete_surrounding_text;
            //   3. insert commit text;
            //   4. apply new preedit.
            var hasDelete = (deleteBeforeChars > 0 || deleteAfterChars > 0)
                            && client.SupportsSurroundingText;
            var hasCommit = !string.IsNullOrEmpty(commitText);
            var newPreeditEmpty = string.IsNullOrEmpty(preeditText);

            // Clear the existing preedit before we mutate the document with
            // delete or commit (it's about to be replaced anyway, and any
            // ongoing composition needs to be torn down first).
            if (_hasPreedit && (hasDelete || hasCommit))
            {
                client.SetPreeditText(null, null);
                _hasPreedit = false;
            }

            if (hasDelete && input is not null)
            {
                // Select the to-be-deleted range, then synthesize a Backspace
                // keypress. TextBox handles "Backspace with non-empty selection"
                // by deleting the selection — same pattern iOS uses for
                // IUIKeyInput.DeleteBackward (see TextInputResponder.cs).
                var sel = client.Selection;
                var anchor = Math.Min(sel.Start, sel.End);
                var cursor = Math.Max(sel.Start, sel.End);
                client.Selection = new TextSelection(
                    Math.Max(0, anchor - deleteBeforeChars),
                    cursor + deleteAfterChars);

                input.Invoke(new RawKeyEventArgs(_owner._keyboard, 0, inputRoot,
                    RawKeyEventType.KeyDown, Key.Back, RawInputModifiers.None,
                    PhysicalKey.Backspace, "\b"));
                input.Invoke(new RawKeyEventArgs(_owner._keyboard, 0, inputRoot,
                    RawKeyEventType.KeyUp, Key.Back, RawInputModifiers.None,
                    PhysicalKey.Backspace, "\b"));
            }

            if (hasCommit)
                input?.Invoke(new RawTextInputEventArgs(_owner._keyboard, 0, inputRoot, commitText!));

            if (!newPreeditEmpty)
            {
                _hasPreedit = true;
                client.SetPreeditText(preeditText,
                    preeditCursorEndChar < 0 ? null : preeditCursorEndChar);
            }
            else if (_hasPreedit)
            {
                // No new preedit: clear any leftover composition.
                client.SetPreeditText(null, null);
                _hasPreedit = false;
            }
        }

        private void OnClientSurroundingTextChanged(object? sender, EventArgs e) => PushSurrounding();
        private void OnClientSelectionChanged(object? sender, EventArgs e) => PushSurrounding();

        private void PushSurrounding()
        {
            if (_client is not { SupportsSurroundingText: true } client)
                return;
            var text = client.SurroundingText ?? string.Empty;
            var selection = client.Selection;
            _owner._surfaceProxy?.SetTextInputSurroundingText(text, selection.End, selection.Start);
        }

        private void UnsubscribeClient()
        {
            if (_client is null)
                return;
            _client.SurroundingTextChanged -= OnClientSurroundingTextChanged;
            _client.SelectionChanged -= OnClientSelectionChanged;
        }
    }
}
