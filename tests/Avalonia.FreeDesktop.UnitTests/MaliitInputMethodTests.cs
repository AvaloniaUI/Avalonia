using Avalonia.Controls.Platform;
using Avalonia.FreeDesktop.DBusIme.Maliit;
using Avalonia.Input.TextInput;
using Avalonia.Threading;
using Avalonia.UnitTests;

namespace Avalonia.FreeDesktop.UnitTests;

public class MaliitInputMethodTests : ScopedTestBase
{
    [Fact]
    public void CommitsAndStateAreAppliedBeforeAcknowledgement()
    {
        using var app = UnitTestApplication.Start(TestServices.MockThreadingInterface);
        var session = new FakeSession();
        var factory = new MaliitInputMethodFactory(() => session);
        using var method = new MaliitX11TextInputMethod(factory, (IntPtr)42);
        var client = new Client { Text = "hello", Selection = new(5, 5) };
        method.SetWindowActive(true);
        method.SetClient(client);
        var state = Dispatch(session, new CommitText("!", 0, 0, -1));
        Assert.Equal("hello!", client.Text);
        Assert.Equal("hello!", state!.SurroundingText);
        Assert.Equal(6, state.Cursor);
        method.SetClient(null);
        Assert.True(factory.PendingDismissal.IsCompletedSuccessfully);
        Assert.Equal(1, session.HideCount);
        Assert.True(session.Disposed);
    }

    [Fact]
    public void ResetCommitsCompositionOnceAndRejectsCallbacksDuringReset()
    {
        using var app = UnitTestApplication.Start(TestServices.MockThreadingInterface);
        var session = new FakeSession { ResetCompletion = new() };
        var factory = new MaliitInputMethodFactory(() => session);
        using var method = new MaliitX11TextInputMethod(factory, (IntPtr)1);
        var client = new Client();
        method.SetWindowActive(true);
        method.SetClient(client);
        Dispatch(session, new PreeditText("ciao", 0, 0, 4, 0));
        Assert.Equal("", client.Text);
        method.Reset();
        Assert.Equal("ciao", client.Text);
        Dispatch(session, new CommitText("ciao", 0, 0, -1));
        Assert.Equal("ciao", client.Text);
        session.ResetCompletion.SetResult();
        Dispatcher.UIThread.RunJobs();
        Assert.Null(client.Preedit);
    }

    [Fact]
    public void ExternalTextChangeCancelsCompositionAndRetapReopensKeyboard()
    {
        using var app = UnitTestApplication.Start(TestServices.MockThreadingInterface);
        var session = new FakeSession();
        using var method = new MaliitX11TextInputMethod(new(() => session), (IntPtr)1);
        var client = new Client();
        method.SetWindowActive(true);
        method.SetClient(client);
        Dispatch(session, new PreeditText("old", 0, 0, 3, 0));
        client.Replace("new");
        method.Reset();
        Assert.Equal("new", client.Text);
        Assert.Null(client.Preedit);
        Dispatch(session, new InputHidden());
        client.Activate();
        Assert.Equal(1, session.ShowCount);
    }

    [Fact]
    public void DetachedCallbacksCannotEditNewClient()
    {
        using var app = UnitTestApplication.Start(TestServices.MockThreadingInterface);
        var sessions = new Queue<FakeSession>([new(), new()]);
        var first = sessions.Peek();
        var factory = new MaliitInputMethodFactory(() => sessions.Dequeue());
        using var method = new MaliitX11TextInputMethod(factory, (IntPtr)1);
        var old = new Client();
        method.SetWindowActive(true);
        method.SetClient(old);
        var callback = first.DispatchAsync!;
        var current = new Client();
        method.SetClient(current);
        var pending = callback(new CommitText("stale", 0, 0, -1));
        Dispatcher.UIThread.RunJobs();
        Assert.True(pending.IsCompletedSuccessfully);
        Assert.Equal("", old.Text);
        Assert.Equal("", current.Text);
        Assert.True(first.Disposed);
    }

    [Fact]
    public void AreaIntersectsClientAndRecomputesAfterResizeAndMove()
    {
        using var app = UnitTestApplication.Start(TestServices.MockThreadingInterface);
        var session = new FakeSession();
        using var method = new MaliitX11TextInputMethod(new(() => session), (IntPtr)1);
        method.SetWindowActive(true);
        method.SetClient(new Client());
        method.UpdateWindowInfo(new PixelPoint(0, 90), 3);
        method.UpdateClientSize(new Size(408, 800));
        method.SetCursorRect(new Rect(10, 20, 2, 16));
        var state = Dispatch(session, new InputAreaChanged(new(0, 1800, 1224, 900)));
        Assert.Equal(new Rect(0, 570, 408, 230), method.OccludedRect);
        Assert.Equal(new InputArea(30, 150, 6, 48), state!.CursorRectangle);
        Assert.Equal(InputPaneState.Open, method.State);
        method.UpdateClientSize(new Size(408, 570));
        Assert.Equal(default, method.OccludedRect);
        Assert.Equal(InputPaneState.Closed, method.State);
        method.UpdateClientSize(new Size(408, 800));
        method.UpdateWindowInfo(new PixelPoint(0, 0), 3);
        Assert.Equal(new Rect(0, 600, 408, 200), method.OccludedRect);
    }

    [Fact]
    public void SignalOnlyKeyDoesNotEditOrForward()
    {
        using var app = UnitTestApplication.Start(TestServices.MockThreadingInterface);
        var session = new FakeSession();
        using var method = new MaliitX11TextInputMethod(new(() => session), (IntPtr)1);
        var client = new Client();
        var forwarded = 0;
        method.ForwardKey += _ => forwarded++;
        method.SetWindowActive(true);
        method.SetClient(client);
        Dispatch(session, new InputKey(6, 0x01000003, 0, "x", false, 1, 1));
        Assert.Equal(0, forwarded);
        Assert.Equal("", client.Text);
    }

    [Fact]
    public void Unknown_Window_Position_Reports_Visibility_Without_Fabricating_Geometry()
    {
        using var app = UnitTestApplication.Start(TestServices.MockThreadingInterface);
        var session = new FakeSession();
        using var method = new MaliitTextInputMethod(new(() => session), 0, hasGlobalCoordinates: false);
        method.UpdateClientSize(new Size(408, 900));
        method.SetWindowActive(true);
        method.SetClient(new Client());
        var state = Dispatch(session, new InputAreaChanged(new(0, 1800, 1224, 900)));
        Assert.Equal(InputPaneState.Open, method.State);
        Assert.Equal(default, method.OccludedRect);
        Assert.False(state!.ToProperties().ContainsKey("winId"));
        Assert.False(state.ToProperties().ContainsKey("cursorRectangle"));

        method.UpdateWindowInfo(default(PixelPoint), 3);
        Assert.Equal(new Rect(0, 600, 408, 300), method.OccludedRect);
        method.UpdateWindowInfo(null, 3);
        Assert.Equal(InputPaneState.Open, method.State);
        Assert.Equal(default, method.OccludedRect);
        Dispatch(session, new InputHidden());
        Assert.Equal(InputPaneState.Closed, method.State);
    }

    [Fact]
    public void Shared_Input_Method_Commits_Without_An_X11_Window()
    {
        using var app = UnitTestApplication.Start(TestServices.MockThreadingInterface);
        var session = new FakeSession();
        using var method = new MaliitTextInputMethod(new(() => session), 0, hasGlobalCoordinates: false);
        var client = new Client();
        method.SetWindowActive(true);
        method.SetClient(client);
        Dispatch(session, new PreeditText("hello", 0, 0, 5, 0));
        method.SetWindowActive(false);
        Assert.Equal("hello", client.Text);
        Assert.Null(client.Preedit);
        Assert.True(session.Disposed);
    }

    private static MaliitWidgetState? Dispatch(FakeSession session, MaliitEvent input)
    {
        var pending = session.DispatchAsync!(input);
        Dispatcher.UIThread.RunJobs();
        return pending.GetAwaiter().GetResult();
    }

    private sealed class FakeSession : IMaliitSession
    {
        public Func<MaliitEvent, Task<MaliitWidgetState?>>? DispatchAsync { get; set; }
        public bool IsConnected { get; private set; }
        public bool Disposed { get; private set; }
        public int HideCount, ShowCount;
        public TaskCompletionSource? ResetCompletion;
        public Task OpenAsync(MaliitWidgetState state, CancellationToken token) { IsConnected = true; return Task.CompletedTask; }
        public Task UpdateAsync(MaliitWidgetState state, CancellationToken token) => Task.CompletedTask;
        public Task ShowAsync(CancellationToken token) { ShowCount++; return Task.CompletedTask; }
        public Task HideAsync(CancellationToken token) { HideCount++; return Task.CompletedTask; }
        public Task ResetAsync(CancellationToken token) => ResetCompletion?.Task ?? Task.CompletedTask;
        public void Dispose() { Disposed = true; IsConnected = false; }
    }

    private sealed class Client : TextInputMethodClient
    {
        public string Text = "";
        public string? Preedit;
        public override Visual TextViewVisual { get; } = new();
        public override bool SupportsPreedit => true;
        public override bool SupportsSurroundingText => true;
        public override string SurroundingText => Text;
        public override Rect CursorRectangle => default;
        public override TextSelection Selection { get; set; }
        public override void SetPreeditText(string? text, int? cursorPos) => Preedit = text;
        public override bool CommitText(string text, int start, int length, int cursor)
        {
            var position = Math.Min(Selection.Start, Selection.End);
            Text = Text.Remove(position, Math.Abs(Selection.End - Selection.Start)).Insert(position, text);
            Selection = new(position + text.Length, position + text.Length);
            RaiseSurroundingTextChanged();
            return true;
        }
        public void Replace(string text) { Text = text; Selection = new(0, 0); RaiseSurroundingTextChanged(); }
        public void Activate() => RaiseInputPaneActivationRequested();
    }
}
