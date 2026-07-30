using System;
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

        [ThreadStatic] private static TsfThreadManager? t_current;
        [ThreadStatic] private static bool t_creationFailed;

        private readonly ITfThreadMgrEx _threadManager;
        private readonly uint _clientId;
        private ITfDocumentMgr? _documentManager;
        private ITfContext? _context;
        private TsfTextStore? _textStore;
        private IntPtr _associatedHwnd;

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
            _threadManager = CreateInstance<ITfThreadMgrEx>(
                in s_clsidThreadMgr,
                MicroComRuntime.GetGuidFor(typeof(ITfThreadMgrEx)));

            uint clientId;
            _threadManager.ActivateEx(&clientId, 0);
            _clientId = clientId;

            Logger.TryGet(LogEventLevel.Debug, LogArea.TextInput)
                ?.Log(this, "TSF thread manager activated, client id {ClientId}", _clientId);
        }

        /// <summary>
        /// Tracks the focused client for a window: a structured client gets the document
        /// manager associated (TSF sees an edit field), anything else clears the
        /// association so TSF treats the window as non-editable again.
        /// </summary>
        public void NotifyClient(IntPtr hwnd, TextInputMethodClient? client)
        {
            if (client is IStructuredTextInput structuredClient && hwnd != IntPtr.Zero)
            {
                AssociateWindow(hwnd);
                _textStore?.SetClient(structuredClient);
            }
            else
            {
                _textStore?.SetClient(null);
                ClearAssociation();
            }
        }

        private unsafe void AssociateWindow(IntPtr hwnd)
        {
            if (_documentManager is null)
            {
                _documentManager = _threadManager.CreateDocumentMgr();

                // The store owns the context: TSF query-interfaces the context owner for
                // ITextStoreACP2 and drives the document through it.
                _textStore = new TsfTextStore();
                IntPtr contextPtr;
                _documentManager.CreateContext(_clientId, 0, _textStore, &contextPtr);
                _context = MicroComRuntime.CreateProxyFor<ITfContext>(contextPtr, true);
                _documentManager.Push(_context);
            }

            if (_associatedHwnd == hwnd)
            {
                return;
            }

            if (_associatedHwnd != IntPtr.Zero)
            {
                _threadManager.AssociateFocus(_associatedHwnd, null)?.Dispose();
            }

            _threadManager.AssociateFocus(hwnd, _documentManager)?.Dispose();
            _associatedHwnd = hwnd;

            Logger.TryGet(LogEventLevel.Debug, LogArea.TextInput)
                ?.Log(this, "TSF document manager associated with window {Hwnd}", hwnd);
        }

        private void ClearAssociation()
        {
            if (_associatedHwnd == IntPtr.Zero)
            {
                return;
            }

            _threadManager.AssociateFocus(_associatedHwnd, null)?.Dispose();
            _associatedHwnd = IntPtr.Zero;

            Logger.TryGet(LogEventLevel.Debug, LogArea.TextInput)
                ?.Log(this, "TSF document manager association cleared");
        }

        public void Dispose()
        {
            _textStore?.SetClient(null);
            ClearAssociation();

            if (_documentManager is not null)
            {
                _documentManager.Pop(1); // TF_POPF_ALL
                _context?.Dispose();
                _documentManager.Dispose();
                _context = null;
                _documentManager = null;
            }

            _textStore?.Dispose();
            _textStore = null;

            _threadManager.Deactivate();
            _threadManager.Dispose();

            if (ReferenceEquals(t_current, this))
            {
                t_current = null;
            }
        }
    }
}
