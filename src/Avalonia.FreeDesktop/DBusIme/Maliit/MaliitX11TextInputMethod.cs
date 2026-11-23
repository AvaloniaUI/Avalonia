using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Logging;
using Avalonia.Threading;

namespace Avalonia.FreeDesktop.DBusIme.Maliit;

internal sealed class MaliitInputMethodFactory : IX11InputMethodFactory
{
    // A terminal hide must finish before another window activates its context.
    internal Task PendingDismissal { get; set; } = Task.CompletedTask;
    internal InputArea LastArea { get; set; }
    internal Func<IMaliitSession> CreateSession { get; }

    internal MaliitInputMethodFactory(Func<IMaliitSession>? createSession = null)
        => CreateSession = createSession ?? (() => new MaliitSession());

    public (ITextInputMethodImpl method, IX11InputMethodControl control) CreateClient(IntPtr xid)
    {
        var method = new MaliitX11TextInputMethod(this, xid);
        return (method, method);
    }
}

internal sealed class MaliitX11TextInputMethod : InputPaneBase, ITextInputMethodImpl,
    IX11InputMethodControl, IX11InputMethodWithInputPane, IX11SynchronousInputMethodControl
{
    private sealed class Lease(TextInputMethodClient client, IMaliitSession session)
    {
        internal TextInputMethodClient Client { get; } = client;
        internal IMaliitSession Session { get; } = session;
        internal CancellationTokenSource Closed { get; } = new();
        internal Task Ready { get; set; } = Task.CompletedTask;
        internal bool Applying, Updating, Dirty;
        internal volatile bool Resetting;
        internal volatile int Epoch;
        internal string Preedit = "";
        internal string CompositionBase = "";
        internal TextSelection CompositionSelection;
    }

    private readonly MaliitInputMethodFactory _factory;
    private readonly ulong _windowId;
    private TextInputMethodClient? _client;
    private TextInputOptions _options = TextInputOptions.Default;
    private Lease? _lease;
    private PixelPoint _position;
    private double _scaling = 1;
    private Size _clientSize;
    private Rect _cursorRect;
    private InputArea _area;
    private bool _windowActive, _disposed;
    private int _activationVersion;

    internal MaliitX11TextInputMethod(MaliitInputMethodFactory factory, IntPtr windowId)
    {
        _factory = factory;
        _windowId = unchecked((ulong)windowId.ToInt64());
    }

    public event Action<string>? Commit;
    public event Action<X11InputMethodForwardedKey>? ForwardKey;
    public bool IsEnabled => _lease?.Session.IsConnected == true;
    public ValueTask<bool> HandleEventAsync(RawKeyEventArgs args, int keyVal, int keyCode) => new(false);

    public void SetClient(TextInputMethodClient? client)
    {
        if (_client == client)
            return;
        ++_activationVersion;
        EndLease();
        if (_client is { } old)
        {
            old.SurroundingTextChanged -= OnSurroundingTextChanged;
            old.SelectionChanged -= OnStateChanged;
            old.InputPaneActivationRequested -= OnActivationRequested;
        }
        _client = client;
        if (client is not null)
        {
            client.SurroundingTextChanged += OnSurroundingTextChanged;
            client.SelectionChanged += OnStateChanged;
            client.InputPaneActivationRequested += OnActivationRequested;
            Activate();
        }
    }

    public void SetWindowActive(bool active)
    {
        if (_windowActive == active)
            return;
        _windowActive = active;
        ++_activationVersion;
        if (active)
            Activate();
        else
            EndLease();
    }

    public void SetOptions(TextInputOptions options)
    {
        _options = options;
        OnStateChanged(this, EventArgs.Empty);
    }

    public void SetCursorRect(Rect rect)
    {
        _cursorRect = rect;
        OnStateChanged(this, EventArgs.Empty);
    }

    public void UpdateWindowInfo(PixelPoint position, double scaling)
    {
        _position = position;
        _scaling = scaling;
        PublishArea(_area);
        OnStateChanged(this, EventArgs.Empty);
    }

    public void UpdateClientSize(Size size)
    {
        _clientSize = size;
        PublishArea(_area);
    }

    private void OnActivationRequested(object? sender, EventArgs e) => Activate();

    private async void Activate()
    {
        var version = _activationVersion;
        try
        {
            await _factory.PendingDismissal;
            if (_disposed || !_windowActive || _client is null || version != _activationVersion)
                return;
            if (_lease is { } existing)
            {
                await existing.Ready;
                if (_lease == existing)
                    await existing.Session.ShowAsync(existing.Closed.Token);
                return;
            }

            var lease = new Lease(_client, _factory.CreateSession());
            _lease = lease;
            PublishArea(_factory.LastArea);
            lease.Session.DispatchAsync = input => DispatchAsync(lease, input);
            lease.Ready = lease.Session.OpenAsync(BuildState(lease), lease.Closed.Token);
            await lease.Ready;
            if (_lease == lease)
                OnStateChanged(this, EventArgs.Empty);
        }
        catch (OperationCanceledException) { }
        catch (Exception error)
        {
            if (version == _activationVersion)
                Fail(error);
        }
    }

    private MaliitWidgetState BuildState(Lease lease)
    {
        var sensitive = lease.Client.IsSensitive || _options.IsSensitive ||
            _options.ContentType is TextInputContentType.Password or TextInputContentType.Pin;
        var selection = sensitive ? default : lease.Client.Selection;
        var rect = new InputArea(
            _position.X + (int)Math.Floor(_cursorRect.X * _scaling),
            _position.Y + (int)Math.Floor(_cursorRect.Y * _scaling),
            (int)Math.Ceiling(_cursorRect.Width * _scaling), (int)Math.Ceiling(_cursorRect.Height * _scaling));
        return new MaliitWidgetState(_options,
            sensitive || !lease.Client.SupportsSurroundingText ? "" : lease.Client.SurroundingText,
            selection.End, selection.Start, _windowId, rect, sensitive);
    }

    private void OnSurroundingTextChanged(object? sender, EventArgs e)
    {
        if (_lease is { Applying: false, Preedit.Length: > 0 } lease &&
            lease.Client.SurroundingText != lease.CompositionBase)
        {
            // An external replacement invalidates the old composition and in-flight callbacks.
            lease.Preedit = "";
            lease.Client.SetPreeditText(null, null);
            Reset();
        }
        OnStateChanged(sender, e);
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (_lease is not { } lease)
            return;
        lease.Dirty = true;
        if (!lease.Updating)
            Dispatcher.UIThread.Post(() => Update(lease));
    }

    private async void Update(Lease lease)
    {
        if (_lease != lease || lease.Updating)
            return;
        lease.Updating = true;
        try
        {
            await lease.Ready;
            while (_lease == lease && lease.Dirty && lease.Session.IsConnected)
            {
                lease.Dirty = false;
                await lease.Session.UpdateAsync(BuildState(lease), lease.Closed.Token);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception error) { if (_lease == lease) Fail(error); }
        finally { lease.Updating = false; }
    }

    public async void Reset()
    {
        if (_lease is not { } lease || lease.Applying || lease.Resetting)
            return;
        lease.Resetting = true;
        ++lease.Epoch;
        CommitPending(lease, preserveSelection: true);
        try
        {
            await lease.Ready;
            if (_lease == lease)
                await lease.Session.ResetAsync(lease.Closed.Token);
        }
        catch (OperationCanceledException) { }
        catch (Exception error) { if (_lease == lease) Fail(error); }
        finally { lease.Resetting = false; }
    }

    private async Task<MaliitWidgetState?> DispatchAsync(Lease lease, MaliitEvent input)
    {
        var epoch = lease.Epoch;
        var resetting = lease.Resetting;
        try
        {
            return await Dispatcher.UIThread.InvokeAsync<MaliitWidgetState?>(() =>
            {
                if (_disposed || _lease != lease)
                    return null;
                if (input is CommitText or PreeditText or InputKey or InputSelection &&
                    (resetting || lease.Resetting || epoch != lease.Epoch))
                    return BuildState(lease);
                Apply(lease, input);
                return _lease == lease ? BuildState(lease) : null;
            }, DispatcherPriority.Input, lease.Closed.Token);
        }
        catch (OperationCanceledException) { return null; }
    }

    private void Apply(Lease lease, MaliitEvent input)
    {
        lease.Applying = true;
        try
        {
            switch (input)
            {
                case CommitText commit:
                    lease.Preedit = "";
                    ApplyCommit(lease.Client, commit);
                    break;
                case PreeditText preedit:
                    if (preedit.ReplacementLength < 0)
                        return;
                    if (preedit.ReplacementStart != 0 || preedit.ReplacementLength != 0)
                        ApplyCommit(lease.Client, new CommitText("", preedit.ReplacementStart, preedit.ReplacementLength, -1));
                    lease.Client.SetPreeditText(preedit.Text,
                        preedit.CursorPosition < 0 ? null : Math.Clamp(preedit.CursorPosition, 0, preedit.Text.Length));
                    lease.Preedit = preedit.Text;
                    lease.CompositionBase = lease.Client.SurroundingText;
                    lease.CompositionSelection = lease.Client.Selection;
                    break;
                case InputSelection selection:
                    var length = lease.Client.SurroundingText.Length;
                    lease.Client.Selection = new TextSelection(Math.Clamp(selection.Start, 0, length),
                        (int)Math.Clamp((long)selection.Start + selection.Length, 0, length));
                    break;
                case InputKey key:
                    if (key.RequestType != 1 && key.Type is 6 or 7)
                    {
                        if (MaliitKeyMapper.TryMap(key, out var forwarded))
                            ForwardKey?.Invoke(forwarded);
                        else if (key.Type == 6 && key.Text.Length > 0 && (key.Modifiers & 0x1c000000) == 0)
                            ApplyCommit(lease.Client, new CommitText(key.Text, 0, 0, -1));
                    }
                    break;
                case InputAreaChanged changed:
                    _factory.LastArea = changed.Area;
                    PublishArea(changed.Area);
                    break;
                case InputHidden:
                    CommitPending(lease);
                    _factory.LastArea = default;
                    PublishArea(default);
                    break;
                case InputActivationLost:
                case InputDisconnected:
                    // Never wait for a peer method from a callback; retire this context after replying.
                    Dispatcher.UIThread.Post(() => { if (_lease == lease) EndLease(dismiss: false); });
                    break;
            }
        }
        finally { lease.Applying = false; }
    }

    private void ApplyCommit(TextInputMethodClient client, CommitText commit)
    {
        if (commit.ReplacementLength < 0)
            return;
        client.SetPreeditText(null, null);
        if (!client.CommitText(commit.Text, commit.ReplacementStart, commit.ReplacementLength, commit.CursorPosition))
        {
            // Older custom clients still support ordinary text through the window's input route.
            if (commit.ReplacementStart == 0 && commit.ReplacementLength == 0 && commit.Text.Length > 0)
                Commit?.Invoke(commit.Text);
        }
    }

    private void CommitPending(Lease lease, bool preserveSelection = false)
    {
        var text = lease.Preedit;
        lease.Preedit = "";
        if (text.Length == 0 || lease.Client.SurroundingText != lease.CompositionBase)
        {
            lease.Client.SetPreeditText(null, null);
            return;
        }
        var selection = lease.Client.Selection;
        var wasApplying = lease.Applying;
        lease.Applying = true;
        try
        {
            lease.Client.Selection = lease.CompositionSelection;
            ApplyCommit(lease.Client, new CommitText(text, 0, 0, -1));
            if (preserveSelection && selection != lease.CompositionSelection)
            {
                var from = Math.Min(lease.CompositionSelection.Start, lease.CompositionSelection.End);
                var to = Math.Max(lease.CompositionSelection.Start, lease.CompositionSelection.End);
                var inserted = lease.Client.SurroundingText.Length - lease.CompositionBase.Length + to - from;
                int Adjust(int p) => p <= from ? p : p >= to ? p + inserted - (to - from) : from + inserted;
                lease.Client.Selection = new TextSelection(Adjust(selection.Start), Adjust(selection.End));
            }
        }
        finally { lease.Applying = wasApplying; }
    }

    private void PublishArea(InputArea area)
    {
        _area = area;
        var rect = area.Width > 0 && area.Height > 0 && _scaling > 0
            ? new Rect((area.X - (double)_position.X) / _scaling, (area.Y - (double)_position.Y) / _scaling,
                area.Width / _scaling, area.Height / _scaling).Intersect(new Rect(_clientSize))
            : default;
        if (rect.Width <= 0 || rect.Height <= 0)
            rect = default;
        var state = rect == default ? InputPaneState.Closed : InputPaneState.Open;
        if (rect == OccludedRect && state == State)
            return;
        var previous = OccludedRect;
        OccludedRect = rect;
        State = state;
        OnStateChanged(new InputPaneStateEventArgs(state, previous, rect));
    }

    private void EndLease(bool dismiss = true)
    {
        if (_lease is not { } lease)
            return;
        _lease = null;
        CommitPending(lease);
        PublishArea(default);
        if (dismiss)
            _factory.PendingDismissal = DismissAsync(lease, _factory.PendingDismissal);
        else
        {
            lease.Closed.Cancel();
            lease.Session.Dispose();
        }
    }

    private static async Task DismissAsync(Lease lease, Task previous)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        try
        {
            await previous;
            await lease.Ready.WaitAsync(timeout.Token);
            if (lease.Session.IsConnected)
                await lease.Session.HideAsync(timeout.Token);
        }
        catch (Exception) { /* A failed hide must not keep a stale input context alive. */ }
        finally
        {
            lease.Closed.Cancel();
            lease.Session.Dispose();
        }
    }

    private void Fail(Exception error)
    {
        Logger.TryGet(LogEventLevel.Warning, LogArea.Platform)?.Log(this,
            "Maliit input unavailable ({ErrorType}).", error.GetType().Name);
        EndLease(dismiss: false);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        SetClient(null);
    }
}
