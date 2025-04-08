using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Threading;
using static Avalonia.Win32.Interop.UnmanagedMethods;
namespace Avalonia.Win32;

internal class Win32DispatcherImpl : IControlledDispatcherImpl
{
    private readonly IntPtr _messageWindow;
    private static Thread? s_uiThread;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    public Win32DispatcherImpl(IntPtr messageWindow)
    {
        _messageWindow = messageWindow;
        s_uiThread = Thread.CurrentThread;
    }
    
    public bool CurrentThreadIsLoopThread => s_uiThread == Thread.CurrentThread;
    internal const int SignalW = unchecked((int)0xdeadbeaf);
    internal const int SignalL = unchecked((int)0x12345678);

    public void Signal() =>
        // Messages from PostMessage are always processed before any user input,
        // so Win32 should call us ASAP
        PostMessage(
            _messageWindow,
            (int)WindowsMessage.WM_DISPATCH_WORK_ITEM,
            new IntPtr(SignalW),
            new IntPtr(SignalL));
    
    public void DispatchWorkItem() => Signaled?.Invoke();
    
    public event Action? Signaled;
    public event Action? Timer;

    public void FireTimer() => Timer?.Invoke();

    public void UpdateTimer(long? dueTimeInMs)
    {
        if (dueTimeInMs == null)
        {
            KillTimer(_messageWindow, (IntPtr)Win32Platform.TIMERID_DISPATCHER);
        }
        else
        {
            var interval = (uint)Math.Min(int.MaxValue - 10, Math.Max(1, Now - dueTimeInMs.Value));
            SetTimer(
                _messageWindow,
                (IntPtr)Win32Platform.TIMERID_DISPATCHER,
                interval,
                null!);
        }
    }

    public bool CanQueryPendingInput => true;
    
    public bool HasPendingInput
    {
        get
        {
            // We need to know if there is any pending input in the Win32
            // queue because we want to only process Avalon "background"
            // items after Win32 input has been processed.
            //
            // Win32 provides the GetQueueStatus API -- but it has a major
            // drawback: it only counts "new" input.  This means that
            // sometimes it could return false, even if there really is input
            // that needs to be processed.  This results in very hard to
            // find bugs.
            //
            // Luckily, Win32 also provides the MsgWaitForMultipleObjectsEx
            // API.  While more awkward to use, this API can return queue
            // status information even if the input is "old".  The various
            // flags we use are:
            //
            // QS_INPUT
            // This represents any pending input - such as mouse moves, or
            // key presses.  It also includes the new GenericInput messages.
            //
            // QS_EVENT
            // This is actually a private flag that represents the various
            // events that can be queued in Win32.  Some of these events
            // can cause input, but Win32 doesn't include them in the
            // QS_INPUT flag.  An example is WM_MOUSELEAVE.
            //
            // QS_POSTMESSAGE
            // If there is already a message in the queue, we need to process
            // it before we can process input.
            //
            // MWMO_INPUTAVAILABLE
            // This flag indicates that any input (new or old) is to be
            // reported.
            //

            return MsgWaitForMultipleObjectsEx(0, null, 0,
                QueueStatusFlags.QS_INPUT | QueueStatusFlags.QS_EVENT | QueueStatusFlags.QS_POSTMESSAGE,
                MsgWaitForMultipleObjectsFlags.MWMO_INPUTAVAILABLE) == 0;
        }
    }

    public void RunLoop(CancellationToken cancellationToken)
    {
        var result = 0;
        while (!cancellationToken.IsCancellationRequested 
               && (result = GetMessage(out var msg, IntPtr.Zero, 0, 0)) > 0)
        {
            bool handled = PreTranslateMessage(msg);
            if (!handled)
                TranslateMessage(ref msg);

            DispatchMessage(ref msg);
        }
        if (result < 0)
        {
            Logging.Logger.TryGet(Logging.LogEventLevel.Error, Logging.LogArea.Win32Platform)
                ?.Log(this, "Unmanaged error in {0}. Error Code: {1}", nameof(RunLoop), Marshal.GetLastWin32Error());
        }
    }

    private bool _enteringAltCode;

    /// <summary>
    /// Called in the message loop before <see cref="TranslateMessage"/>.
    /// </summary>
    /// <param name="msg">The message received from the thread's message queue.</param>
    /// <returns>
    /// <c>true</c> if the message was handled and <see cref="TranslateMessage"/> should be skipped.
    /// </returns>
    private bool PreTranslateMessage(MSG msg)
    {
        bool skipTranslateMessage = false;
        var message = (WindowsMessage)msg.message;
        switch (message)
        {
            case WindowsMessage.WM_KEYDOWN:
            {
                _enteringAltCode = false;
                break;
            }
            case WindowsMessage.WM_KEYUP:
            {
                var virtualKey = (VirtualKeyStates)msg.wParam;
                if (virtualKey == VirtualKeyStates.VK_MENU)
                {
                    // End of ALT key combination:
                    // - Call TranslateMessage() when ALT code (numpad keys) was entered.
                    // - Skip TranslateMessage() when other ALT combination was entered.
                    skipTranslateMessage = !_enteringAltCode;
                }

                _enteringAltCode = false;
                break;
            }
            case WindowsMessage.WM_SYSKEYDOWN:
            case WindowsMessage.WM_SYSKEYUP:
            {
                // Skip TranslateMessage() while user enters ALT combination.
                bool isAltDown = IsAltDown();
                skipTranslateMessage = isAltDown;

                var virtualKey = (VirtualKeyStates)msg.wParam;
                if (isAltDown && virtualKey >= VirtualKeyStates.VK_NUMPAD0 
                              && virtualKey <= VirtualKeyStates.VK_NUMPAD9)
                {
                    _enteringAltCode = true;
                }

                break;
            }
            case WindowsMessage.WM_CHAR:
            {
                _enteringAltCode = false;
                break;
            }
        }

        return skipTranslateMessage;

        static bool IsAltDown() => (GetKeyState((int)VirtualKeyStates.VK_MENU) & 0x8000) != 0;
    }

    public long Now => _clock.ElapsedMilliseconds;
}
