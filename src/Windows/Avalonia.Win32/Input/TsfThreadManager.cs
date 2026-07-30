using System;
using System.Runtime.InteropServices;
using Avalonia.Input.TextInput;
using Avalonia.Logging;
using Avalonia.Win32.Input.Tsf;
using MicroCom.Runtime;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32.Input
{
    /// <summary>
    /// Owns the thread's Text Services Framework activation: the thread manager, its
    /// client id, and a document manager whose context is owned by the
    /// <see cref="TsfTextStore"/> and associated with the focused window while a
    /// structured client has focus, so text services talk to the focused control through
    /// the store.
    /// </summary>
    internal sealed class TsfThreadManager : IDisposable
    {
        private static readonly Guid s_clsidThreadMgr = new("529a9e6b-6587-4f23-ab9e-9c7d683e3c50");
        private static readonly Guid s_systemFunctionProvider = new("9a698bb0-0f21-11d3-8df1-00105a2799b5");

        [ThreadStatic] private static TsfThreadManager? t_current;
        [ThreadStatic] private static bool t_creationFailed;

        private readonly ITfThreadMgrEx _threadManager;
        private readonly ITfKeystrokeMgr? _keystrokeManager;
        private readonly uint _clientId;
        private ITfDocumentMgr? _documentManager;
        private ITfContext? _context;
        private TsfTextStore? _textStore;
        private uint _textEditSinkCookie;
        private bool _hasTextEditSinkCookie;
        private IntPtr _associatedHwnd;

        /// <summary>
        /// The thread's manager only if one has already been created; consulting this
        /// never activates TSF. Teardown and routing guards use it so a window that never
        /// hosted a structured client cannot trigger an activation.
        /// </summary>
        public static TsfThreadManager? Existing => t_current;

        /// <summary>
        /// The thread's manager, created on first use when
        /// <see cref="Win32PlatformOptions.UseTsfTextInput"/> is enabled; null when the
        /// integration is off or activation failed (in which case IMM carries on alone).
        /// </summary>
        public static TsfThreadManager? Current
        {
            get
            {
                if (t_current is not null)
                {
                    return t_current;
                }

                if (t_creationFailed || !Win32Platform.Options.UseTsfTextInput)
                {
                    return null;
                }

                try
                {
                    t_current = new TsfThreadManager();
                }
                catch (Exception exception)
                {
                    t_creationFailed = true;
                    Logger.TryGet(LogEventLevel.Warning, LogArea.TextInput)
                        ?.Log(null, "TSF thread manager activation failed: {Error}", exception);
                }

                return t_current;
            }
        }

        private unsafe TsfThreadManager()
        {
            _threadManager = GetExistingThreadManager() ?? CreateInstance<ITfThreadMgrEx>(
                in s_clsidThreadMgr,
                MicroComRuntime.GetGuidFor(typeof(ITfThreadMgrEx)));

            // ActivateEx is still required on an existing manager: it is the only call
            // that returns our TfClientId (CreateContext's tidOwner), and it takes our
            // own reference on the refcounted activation so another activator letting go
            // cannot deactivate TSF under us. On an already-active thread it reports
            // S_FALSE with the id - a success code the throwing proxy would surface as an
            // exception, so the slot is called directly and the result stays data.
            uint clientId = 0;
            var hr = ActivateThreadManager(_threadManager, &clientId);
            if (hr < 0)
            {
                throw new COMException("ActivateEx failed", hr);
            }

            _clientId = clientId;

            try
            {
                _keystrokeManager = _threadManager.QueryInterface<ITfKeystrokeMgr>();
            }
            catch (Exception exception)
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.TextInput)
                    ?.Log(this, "TSF keystroke manager unavailable: {Error}", exception.Message);
            }

            Logger.TryGet(LogEventLevel.Debug, LogArea.TextInput)
                ?.Log(this, "TSF thread manager activated, client id {ClientId}", _clientId);
        }

        /// <summary>
        /// The thread's manager is a per-thread singleton that CUAS and the input pane
        /// often create before we do; prefer the existing instance over creating one.
        /// Returns null when the thread has none yet (TF_GetThreadMgr reports S_FALSE).
        /// </summary>
        private static ITfThreadMgrEx? GetExistingThreadManager()
        {
            try
            {
                if (TF_GetThreadMgr(out var existing) != 0 || existing == IntPtr.Zero)
                {
                    return null;
                }

                using var threadManager = MicroComRuntime.CreateProxyFor<ITfThreadMgr>(existing, true);
                return threadManager.QueryInterface<ITfThreadMgrEx>();
            }
            catch (Exception)
            {
                return null;
            }
        }

        [DllImport("msctf.dll", ExactSpelling = true)]
        private static extern int TF_GetThreadMgr(out IntPtr pptim);

        // Vtable slot 14 = IUnknown (3) + the eleven ITfThreadMgr methods.
        private static unsafe int ActivateThreadManager(ITfThreadMgrEx threadManager, uint* clientId)
        {
            using var lease = MicroComRuntime.LeaseNativePointerForCall(threadManager);
            return ((delegate* unmanaged[Stdcall]<void*, uint*, uint, int>)(*(void***)lease.Pointer)[14])(lease.Pointer, clientId, 0);
        }

        /// <summary>
        /// Releases the association when the given window is going away; associations
        /// held by other, still-living windows are untouched.
        /// </summary>
        public void NotifyWindowDestroyed(IntPtr hwnd)
        {
            if (_associatedHwnd != hwnd)
            {
                return;
            }

            _textStore?.SetClient(null);
            _textStore?.SetWindow(IntPtr.Zero, null);
            ClearAssociation();
        }

        /// <summary>
        /// Tracks the focused client for a window: a structured client gets the document
        /// manager associated (TSF sees an edit field), anything else clears the
        /// association so TSF treats the window as non-editable again.
        /// </summary>
        public void NotifyClient(IntPtr hwnd, WindowImpl? window, TextInputMethodClient? client)
        {
            if (client is IStructuredTextInput structuredClient && hwnd != IntPtr.Zero)
            {
                EnsureDocumentManager();

                // A composition against the previous client must terminate while that
                // client is still attached and writable - the terminating edit session
                // wants to finalize its text.
                if (_textStore is { HasActiveComposition: true } store
                    && !ReferenceEquals(store.Client, structuredClient))
                {
                    TerminateCompositions();
                }

                // The client attaches before focus lands on the document: setting the
                // focus makes the IME inspect the store immediately, and a first
                // impression of an empty read-only document would park it in direct
                // input.
                _textStore?.SetWindow(hwnd, window);
                _textStore?.SetClient(structuredClient);

                AssociateWindow(hwnd);
            }
            else
            {
                // Dropping the association first lets TSF terminate any active
                // composition against the still-attached, still-writable client; the
                // final edit of that termination failed with TS_E_READONLY when the
                // client detached first.
                ClearAssociation();
                _textStore?.SetClient(null);
                _textStore?.SetWindow(IntPtr.Zero, null);
            }
        }

        /// <summary>
        /// Asks TSF to end every composition in the context; used before the store's
        /// client changes so the terminating edit lands in the right document.
        /// </summary>
        private void TerminateCompositions()
        {
            if (_context is null)
            {
                return;
            }

            try
            {
                using var services = _context.QueryInterface<ITfContextOwnerCompositionServices>();
                services.TerminateComposition(null);
            }
            catch (Exception exception)
            {
                Logger.TryGet(LogEventLevel.Debug, LogArea.TextInput)
                    ?.Log(this, "TSF composition termination failed: {Error}", exception.Message);
            }
        }

        private unsafe void EnsureDocumentManager()
        {
            if (_documentManager is not null)
            {
                return;
            }

            _documentManager = _threadManager.CreateDocumentMgr();

            // The store owns the context: TSF query-interfaces the context owner for
            // ITextStoreACP2 (and the composition sink) and drives the document
            // through it.
            _textStore = new TsfTextStore();
            IntPtr contextPtr;
            _documentManager.CreateContext(_clientId, 0, _textStore, &contextPtr);
            _context = MicroComRuntime.CreateProxyFor<ITfContext>(contextPtr, true);
            _documentManager.Push(_context);

            // The text edit sink supplies the read-only edit cookie the store needs
            // to read the composition's display attribute property.
            try
            {
                using var source = _context.QueryInterface<ITfSource>();
                var sinkIid = MicroComRuntime.GetGuidFor(typeof(ITfTextEditSink));
                _textEditSinkCookie = source.AdviseSink(&sinkIid, _textStore);
                _hasTextEditSinkCookie = true;
            }
            catch (Exception exception)
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.TextInput)
                    ?.Log(this, "TSF text edit sink advise failed: {Error}", exception.Message);
            }
        }

        private void AssociateWindow(IntPtr hwnd)
        {
            try
            {
                if (_associatedHwnd != hwnd)
                {
                    if (_associatedHwnd != IntPtr.Zero)
                    {
                        _threadManager.AssociateFocus(_associatedHwnd, null)?.Dispose();
                    }

                    _threadManager.AssociateFocus(hwnd, _documentManager)?.Dispose();
                    _associatedHwnd = hwnd;

                    Logger.TryGet(LogEventLevel.Debug, LogArea.TextInput)
                        ?.Log(this, "TSF document manager associated with window {Hwnd}", hwnd);
                }

                // AssociateFocus only routes future focus changes, but the window
                // already holds the keyboard focus when a client focuses inside it, so
                // the document focus must be set explicitly or text services never look
                // at the store.
                _threadManager.SetFocus(_documentManager!);
            }
            catch (Exception exception)
            {
                // A window can die between the focus event and the association call;
                // stay unassociated rather than fault the focus change.
                _associatedHwnd = IntPtr.Zero;
                Logger.TryGet(LogEventLevel.Warning, LogArea.TextInput)
                    ?.Log(this, "TSF focus association failed: {Error}", exception.Message);
            }
        }

        /// <summary>
        /// Offers a key message to the active text service before the framework processes
        /// it; true means the service ate the key and the message must not be dispatched
        /// (the already-translated WM_CHAR is suppressed by the caller). Only keys for the
        /// TSF-associated window are offered, so legacy-focused windows are untouched.
        /// </summary>
        public bool FilterKeyMessage(IntPtr hwnd, WindowsMessage message, IntPtr wParam, IntPtr lParam)
        {
            if (_keystrokeManager is null || _associatedHwnd != hwnd)
            {
                return false;
            }

            try
            {
                switch (message)
                {
                    case WindowsMessage.WM_KEYDOWN:
                    case WindowsMessage.WM_SYSKEYDOWN:
                    {
                        var eaten = _keystrokeManager.TestKeyDown(wParam, lParam) != 0
                            && _keystrokeManager.KeyDown(wParam, lParam) != 0;

                        if (eaten)
                        {
                            Logger.TryGet(LogEventLevel.Debug, LogArea.TextInput)
                                ?.Log(this, "TSF ate key down {Key}", wParam);
                        }

                        return eaten;
                    }

                    case WindowsMessage.WM_KEYUP:
                    case WindowsMessage.WM_SYSKEYUP:
                        return _keystrokeManager.TestKeyUp(wParam, lParam) != 0
                            && _keystrokeManager.KeyUp(wParam, lParam) != 0;
                }
            }
            catch (Exception exception)
            {
                Logger.TryGet(LogEventLevel.Debug, LogArea.TextInput)
                    ?.Log(this, "TSF key forwarding failed: {Error}", exception.Message);
            }

            return false;
        }

        /// <summary>
        /// Starts a reconversion of the focused structured client's selection through the
        /// system function provider (the TSF-native counterpart of the IMM
        /// SCS_SETRECONVERTSTRING trigger). False when TSF does not own the focus, there
        /// is no selection, or the active text service declines the range.
        /// </summary>
        public unsafe bool TryReconvert()
        {
            if (_associatedHwnd == IntPtr.Zero || _textStore is null)
            {
                return false;
            }

            var range = _textStore.TryCreateSelectionRange();
            if (range is null)
            {
                return false;
            }

            ITfFunctionProvider? provider = null;
            IUnknown? functionUnknown = null;
            ITfFnReconversion? reconversion = null;
            ITfRange? adjusted = null;
            try
            {
                var providerGuid = s_systemFunctionProvider;
                var providerPtr = _threadManager.GetFunctionProvider(&providerGuid);
                if (providerPtr == IntPtr.Zero)
                {
                    return false;
                }

                provider = MicroComRuntime.CreateProxyFor<ITfFunctionProvider>(providerPtr, true);

                var functionGuid = Guid.Empty;
                var reconversionIid = MicroComRuntime.GetGuidFor(typeof(ITfFnReconversion));
                functionUnknown = provider.GetFunction(&functionGuid, &reconversionIid);
                reconversion = functionUnknown.QueryInterface<ITfFnReconversion>();

                // The service may adjust the requested range to what it can reconvert.
                void* adjustedPtr = null;
                var convertable = reconversion.QueryRange(range, new IntPtr(&adjustedPtr));

                if (adjustedPtr != null)
                {
                    adjusted = MicroComRuntime.CreateProxyFor<ITfRange>(adjustedPtr, true);
                }

                if (convertable == 0)
                {
                    return false;
                }

                reconversion.Reconvert(adjusted ?? range);

                Logger.TryGet(LogEventLevel.Debug, LogArea.TextInput)
                    ?.Log(this, "TSF reconversion started");
                return true;
            }
            catch (Exception exception)
            {
                Logger.TryGet(LogEventLevel.Debug, LogArea.TextInput)
                    ?.Log(this, "TSF reconversion unavailable: {Error}", exception.Message);
                return false;
            }
            finally
            {
                adjusted?.Dispose();
                reconversion?.Dispose();
                functionUnknown?.Dispose();
                provider?.Dispose();
                range.Dispose();
            }
        }

        private void ClearAssociation()
        {
            if (_associatedHwnd == IntPtr.Zero)
            {
                return;
            }

            try
            {
                _threadManager.AssociateFocus(_associatedHwnd, null)?.Dispose();
            }
            catch (Exception)
            {
                // Clearing against an already-destroyed window is fine; the association
                // died with it.
            }

            _associatedHwnd = IntPtr.Zero;

            Logger.TryGet(LogEventLevel.Debug, LogArea.TextInput)
                ?.Log(this, "TSF document manager association cleared");
        }

        public void Dispose()
        {
            _textStore?.SetClient(null);
            ClearAssociation();

            if (_hasTextEditSinkCookie && _context is not null)
            {
                try
                {
                    using var source = _context.QueryInterface<ITfSource>();
                    source.UnadviseSink(_textEditSinkCookie);
                }
                catch (Exception)
                {
                    // The context is going away regardless.
                }

                _hasTextEditSinkCookie = false;
            }

            if (_documentManager is not null)
            {
                try
                {
                    _documentManager.Pop(1); // TF_POPF_ALL
                }
                catch (Exception)
                {
                    // Teardown must not fault on a context TSF already dropped.
                }

                _context?.Dispose();
                _documentManager.Dispose();
                _context = null;
                _documentManager = null;
            }

            _textStore?.Dispose();
            _textStore = null;

            _keystrokeManager?.Dispose();

            try
            {
                _threadManager.Deactivate();
            }
            catch (Exception)
            {
                // Balanced against our activation; a stale thread state is not fatal.
            }

            _threadManager.Dispose();

            if (ReferenceEquals(t_current, this))
            {
                t_current = null;
            }
        }
    }
}
