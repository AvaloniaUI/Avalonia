using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Input.TextInput;
using Avalonia.Logging;
using Avalonia.MicroCom;
using Avalonia.Win32.Interop;
using MicroCom.Runtime;
using static Avalonia.Win32.Input.Tsf.TsfConstants;

namespace Avalonia.Win32.Input.Tsf
{
    /// <summary>
    /// The TSF text store over <see cref="IStructuredTextInput"/>: TSF reads the document,
    /// selection and end position through the navigation core and mutates through
    /// <c>ReplaceText</c>, while app-originated changes reach TSF from the
    /// <c>TextChanged</c> delta and the caret event. Locks are granted synchronously on the
    /// UI thread (TSF drives the store from the thread that activated the thread manager);
    /// notifications raised while a lock is held are queued and flushed after release, and
    /// edits TSF itself performs under a write lock are not echoed back as change
    /// notifications. Geometry and attribute queries arrive in a later phase.
    /// </summary>
    internal sealed unsafe class TsfTextStore : CallbackBase, ITextStoreACP2
    {
        private static readonly Guid s_iidTextStoreAcpSink = MicroComRuntime.GetGuidFor(typeof(ITextStoreACPSink));

        private IStructuredTextInput? _client;
        private ITextStoreACPSink? _sink;
        private uint _sinkMask;
        private uint _activeLockFlags;
        private bool _pendingAsyncLock;
        private uint _pendingAsyncLockFlags;
        private bool _inTsfEdit;
        private readonly List<TS_TEXTCHANGE> _pendingTextChanges = new();
        private bool _pendingSelectionChange;

        /// <summary>
        /// Attaches the store to the focused structured client, or detaches it when focus
        /// moves to a non-structured target. The store itself stays alive with the context;
        /// without a client it reads as an empty read-only document. Swapping clients keeps
        /// the window association, so TSF is told the whole document was replaced.
        /// </summary>
        public void SetClient(IStructuredTextInput? client)
        {
            if (ReferenceEquals(_client, client))
            {
                return;
            }

            var oldEnd = 0;

            if (_client is not null)
            {
                oldEnd = _client.DocumentEnd.Offset;
                _client.TextChanged -= OnClientTextChanged;
                _client.CaretPositionChanged -= OnClientCaretPositionChanged;
            }

            _client = client;

            var newEnd = 0;

            if (_client is not null)
            {
                newEnd = _client.DocumentEnd.Offset;
                _client.TextChanged += OnClientTextChanged;
                _client.CaretPositionChanged += OnClientCaretPositionChanged;
            }

            if (oldEnd != 0 || newEnd != 0)
            {
                var change = new TS_TEXTCHANGE { Start = 0, OldEnd = oldEnd, NewEnd = newEnd };

                if (_activeLockFlags != 0)
                {
                    _pendingTextChanges.Add(change);
                    _pendingSelectionChange = true;
                }
                else
                {
                    NotifyTextChange(change);
                    NotifySelectionChange();
                }
            }
        }

        // App-originated notifications.

        private void OnClientTextChanged(object? sender, TextChange change)
        {
            if (_inTsfEdit)
            {
                return;
            }

            var start = change.Position.Offset;
            var textChange = new TS_TEXTCHANGE
            {
                Start = start,
                OldEnd = start + change.OldLength,
                NewEnd = start + change.NewLength,
            };

            if (_activeLockFlags != 0)
            {
                _pendingTextChanges.Add(textChange);
                return;
            }

            NotifyTextChange(textChange);
        }

        private void OnClientCaretPositionChanged(object? sender, EventArgs e)
        {
            if (_inTsfEdit)
            {
                return;
            }

            if (_activeLockFlags != 0)
            {
                _pendingSelectionChange = true;
                return;
            }

            NotifySelectionChange();
        }

        private void NotifyTextChange(TS_TEXTCHANGE change)
        {
            if (_sink is null || (_sinkMask & TS_AS_TEXT_CHANGE) == 0)
            {
                return;
            }

            try
            {
                _sink.OnTextChange(0, &change);
            }
            catch (Exception exception)
            {
                Trace("OnTextChange failed: {Error}", exception.Message);
            }
        }

        private void NotifySelectionChange()
        {
            if (_sink is null || (_sinkMask & TS_AS_SEL_CHANGE) == 0)
            {
                return;
            }

            try
            {
                _sink.OnSelectionChange();
            }
            catch (Exception exception)
            {
                Trace("OnSelectionChange failed: {Error}", exception.Message);
            }
        }

        private void FlushPendingNotifications()
        {
            if (_pendingTextChanges.Count > 0)
            {
                var changes = _pendingTextChanges.ToArray();
                _pendingTextChanges.Clear();

                foreach (var change in changes)
                {
                    NotifyTextChange(change);
                }
            }

            if (_pendingSelectionChange)
            {
                _pendingSelectionChange = false;
                NotifySelectionChange();
            }
        }

        // Sink management and the lock model.

        public void AdviseSink(Guid* riid, IUnknown punk, uint dwMask)
        {
            if (riid is null || *riid != s_iidTextStoreAcpSink)
            {
                throw new COMException(null, E_INVALIDARG);
            }

            if (_sink is not null)
            {
                // TSF re-advises the existing sink to update its notification mask.
                _sinkMask = dwMask;
                return;
            }

            _sink = punk.QueryInterface<ITextStoreACPSink>();
            _sinkMask = dwMask;
            Trace("Sink advised, mask {Mask}", dwMask);
        }

        public void UnadviseSink(IUnknown punk)
        {
            _sink?.Dispose();
            _sink = null;
            _sinkMask = 0;
            Trace("Sink unadvised");
        }

        public void RequestLock(uint dwLockFlags, IntPtr phrSession)
        {
            if (_sink is null || phrSession == IntPtr.Zero)
            {
                throw new COMException(null, E_UNEXPECTED);
            }

            if (_activeLockFlags != 0)
            {
                if ((dwLockFlags & TS_LF_SYNC) != 0)
                {
                    *(int*)phrSession = TS_E_SYNCHRONOUS;
                    return;
                }

                // Queue the upgrade/followup request; it is granted right after the
                // current lock releases, and the async session result is not reported.
                _pendingAsyncLock = true;
                _pendingAsyncLockFlags = dwLockFlags;
                *(int*)phrSession = TS_S_ASYNC;
                return;
            }

            *(int*)phrSession = GrantLock(dwLockFlags);
        }

        private int GrantLock(uint dwLockFlags)
        {
            _activeLockFlags = dwLockFlags & TS_LF_READWRITE;

            int result;
            try
            {
                _sink!.OnLockGranted(dwLockFlags);
                result = S_OK;
            }
            catch (COMException exception)
            {
                // The proxy surfaces any non-zero HRESULT as an exception; the sink's
                // result is the session result either way.
                result = exception.HResult;
            }
            finally
            {
                _activeLockFlags = 0;
            }

            FlushPendingNotifications();

            if (_pendingAsyncLock)
            {
                _pendingAsyncLock = false;
                GrantLock(_pendingAsyncLockFlags);
            }

            return result;
        }

        private void RequireLock(uint flags)
        {
            if ((_activeLockFlags & flags) != flags)
            {
                throw new COMException(null, TS_E_NOLOCK);
            }
        }

        private IStructuredTextInput RequireEditableClient()
            => _client ?? throw new COMException(null, TS_E_READONLY);

        // Document access. Without a client the store reads as an empty document, so
        // requests in flight around focus transitions get consistent answers instead of
        // errors; mutation attempts report the read-only state.

        public void GetStatus(TS_STATUS* pdcs)
        {
            if (pdcs is null)
            {
                throw new COMException(null, E_INVALIDARG);
            }

            pdcs->DynamicFlags = _client is null ? TS_SD_READONLY : 0;
            pdcs->StaticFlags = TS_SS_NOHIDDENTEXT;
        }

        public void GetEndACP(IntPtr pacp)
        {
            RequireLock(TS_LF_READ);
            *(int*)pacp = _client?.DocumentEnd.Offset ?? 0;
        }

        public void QueryInsert(int acpTestStart, int acpTestEnd, uint cch, IntPtr pacpResultStart, IntPtr pacpResultEnd)
        {
            var length = _client?.DocumentEnd.Offset ?? 0;

            if (acpTestStart < 0 || acpTestStart > acpTestEnd || acpTestEnd > length)
            {
                throw new COMException(null, E_INVALIDARG);
            }

            *(int*)pacpResultStart = acpTestStart;
            *(int*)pacpResultEnd = acpTestEnd;
        }

        public void GetSelection(uint ulIndex, uint ulCount, IntPtr pSelection, IntPtr pcFetched)
        {
            RequireLock(TS_LF_READ);

            *(uint*)pcFetched = 0;

            if (ulCount == 0 || (ulIndex != 0 && ulIndex != TF_DEFAULT_SELECTION))
            {
                return;
            }

            var acp = (TS_SELECTION_ACP*)pSelection;
            acp->Style.InterimChar = 0;

            if (_client is null)
            {
                acp->Start = 0;
                acp->End = 0;
                acp->Style.ActiveSelectionEnd = TS_AE_NONE;
                *(uint*)pcFetched = 1;
                return;
            }

            var selection = _client.Selection;
            var caret = _client.CaretPosition;

            acp->Start = selection.Start.Offset;
            acp->End = selection.End.Offset;
            acp->Style.ActiveSelectionEnd =
                selection.Start.Offset == selection.End.Offset
                    ? TS_AE_NONE
                    : caret.Offset == selection.Start.Offset
                        ? TS_AE_START
                        : TS_AE_END;

            *(uint*)pcFetched = 1;
        }

        public void SetSelection(uint ulCount, IntPtr pSelection)
        {
            RequireLock(TS_LF_READWRITE);

            if (ulCount != 1)
            {
                throw new COMException(null, E_INVALIDARG);
            }

            var client = RequireEditableClient();
            var acp = (TS_SELECTION_ACP*)pSelection;

            _inTsfEdit = true;
            try
            {
                client.Selection = client.RangeAt(acp->Start, acp->End - acp->Start);
            }
            finally
            {
                _inTsfEdit = false;
            }
        }

        public void GetText(int acpStart, int acpEnd, IntPtr pchPlain, uint cchPlainReq, IntPtr pcchPlainRet, IntPtr prgRunInfo, uint cRunInfoReq, IntPtr pcRunInfoRet, IntPtr pacpNext)
        {
            RequireLock(TS_LF_READ);
            var text = _client is null ? string.Empty : _client.GetText(_client.DocumentRange);

            if (acpEnd < 0)
            {
                acpEnd = text.Length;
            }

            if (acpStart < 0 || acpStart > text.Length || acpEnd < acpStart || acpEnd > text.Length)
            {
                throw new COMException(null, TS_E_INVALIDPOS);
            }

            // A run-info-only call (no text buffer requested) still walks the whole span;
            // otherwise the text buffer bounds what this call covers.
            var available = acpEnd - acpStart;
            var covered = cchPlainReq > 0 ? Math.Min(available, (int)cchPlainReq) : available;
            var copied = Math.Min(covered, (int)cchPlainReq);

            if (pchPlain != IntPtr.Zero && copied > 0)
            {
                fixed (char* source = text)
                {
                    Buffer.MemoryCopy(source + acpStart, (void*)pchPlain, (long)cchPlainReq * sizeof(char), (long)copied * sizeof(char));
                }
            }

            if (pcchPlainRet != IntPtr.Zero)
            {
                *(uint*)pcchPlainRet = (uint)copied;
            }

            if (pcRunInfoRet != IntPtr.Zero)
            {
                if (cRunInfoReq > 0 && prgRunInfo != IntPtr.Zero && covered > 0)
                {
                    var runInfo = (TS_RUNINFO*)prgRunInfo;
                    runInfo->Count = (uint)covered;
                    runInfo->Type = TS_RT_PLAIN;
                    *(uint*)pcRunInfoRet = 1;
                }
                else
                {
                    *(uint*)pcRunInfoRet = 0;
                }
            }

            if (pacpNext != IntPtr.Zero)
            {
                *(int*)pacpNext = acpStart + covered;
            }
        }

        public void SetText(uint dwFlags, int acpStart, int acpEnd, IntPtr pchText, uint cch, IntPtr pChange)
        {
            RequireLock(TS_LF_READWRITE);
            var client = RequireEditableClient();
            var length = client.DocumentEnd.Offset;

            if (acpStart < 0 || acpStart > acpEnd || acpEnd > length)
            {
                throw new COMException(null, TS_E_INVALIDPOS);
            }

            var text = cch == 0 || pchText == IntPtr.Zero
                ? string.Empty
                : new string((char*)pchText, 0, (int)cch);

            _inTsfEdit = true;
            try
            {
                client.ReplaceText(client.RangeAt(acpStart, acpEnd - acpStart), text);
            }
            finally
            {
                _inTsfEdit = false;
            }

            if (pChange != IntPtr.Zero)
            {
                var change = (TS_TEXTCHANGE*)pChange;
                change->Start = acpStart;
                change->OldEnd = acpEnd;
                change->NewEnd = acpStart + text.Length;
            }
        }

        public void InsertTextAtSelection(uint dwFlags, IntPtr pchText, uint cch, IntPtr pacpStart, IntPtr pacpEnd, IntPtr pChange)
        {
            RequireLock((dwFlags & TS_IAS_QUERYONLY) != 0 ? TS_LF_READ : TS_LF_READWRITE);

            var client = RequireEditableClient();
            var selection = client.Selection;
            var start = selection.Start.Offset;
            var end = selection.End.Offset;

            var text = cch == 0 || pchText == IntPtr.Zero
                ? string.Empty
                : new string((char*)pchText, 0, (int)cch);

            if ((dwFlags & TS_IAS_QUERYONLY) == 0)
            {
                _inTsfEdit = true;
                try
                {
                    client.ReplaceText(client.RangeAt(start, end - start), text);
                }
                finally
                {
                    _inTsfEdit = false;
                }

                if (pChange != IntPtr.Zero)
                {
                    var change = (TS_TEXTCHANGE*)pChange;
                    change->Start = start;
                    change->OldEnd = end;
                    change->NewEnd = start + text.Length;
                }
            }

            if (pacpStart != IntPtr.Zero)
            {
                *(int*)pacpStart = start;
            }

            if (pacpEnd != IntPtr.Zero)
            {
                *(int*)pacpEnd = start + text.Length;
            }
        }

        // Embedded objects and formatted text are not part of the plain-text store;
        // attribute requests report an empty result set rather than failing so text
        // services that probe attributes keep going.

        public void GetFormattedText(int acpStart, int acpEnd, IntPtr ppDataObject)
            => throw new COMException(null, E_NOTIMPL);

        public void GetEmbedded(int acpPos, Guid* rguidService, Guid* riid, IntPtr ppunk)
            => throw new COMException(null, E_NOTIMPL);

        public void QueryInsertEmbedded(IntPtr pguidService, IntPtr pFormatEtc, IntPtr pfInsertable)
        {
            if (pfInsertable != IntPtr.Zero)
            {
                *(int*)pfInsertable = 0;
            }
        }

        public void InsertEmbedded(uint dwFlags, int acpStart, int acpEnd, IntPtr pDataObject, IntPtr pChange)
            => throw new COMException(null, E_NOTIMPL);

        public void InsertEmbeddedAtSelection(uint dwFlags, IntPtr pDataObject, IntPtr pacpStart, IntPtr pacpEnd, IntPtr pChange)
            => throw new COMException(null, E_NOTIMPL);

        public void RequestSupportedAttrs(uint dwFlags, uint cFilterAttrs, IntPtr paFilterAttrs)
        {
        }

        public void RequestAttrsAtPosition(int acpPos, uint cFilterAttrs, IntPtr paFilterAttrs, uint dwFlags)
        {
        }

        public void RequestAttrsTransitioningAtPosition(int acpPos, uint cFilterAttrs, IntPtr paFilterAttrs, uint dwFlags)
        {
        }

        public void FindNextAttrTransition(int acpStart, int acpHalt, uint cFilterAttrs, IntPtr paFilterAttrs, uint dwFlags, IntPtr pacpNext, IntPtr pfFound, IntPtr plFoundOffset)
        {
            if (pfFound != IntPtr.Zero)
            {
                *(int*)pfFound = 0;
            }

            if (plFoundOffset != IntPtr.Zero)
            {
                *(int*)plFoundOffset = 0;
            }
        }

        public void RetrieveRequestedAttrs(uint ulCount, IntPtr paAttrVals, IntPtr pcFetched)
        {
            if (pcFetched != IntPtr.Zero)
            {
                *(uint*)pcFetched = 0;
            }
        }

        // Geometry arrives with the layout phase; a single-view store reports one cookie.

        public void GetActiveView(IntPtr pvcView)
        {
            if (pvcView != IntPtr.Zero)
            {
                *(uint*)pvcView = 1;
            }
        }

        public void GetACPFromPoint(uint vcView, UnmanagedMethods.POINT* ptScreen, uint dwFlags, IntPtr pacp)
            => throw new COMException(null, E_NOTIMPL);

        public void GetTextExt(uint vcView, int acpStart, int acpEnd, UnmanagedMethods.RECT* prc, IntPtr pfClipped)
            => throw new COMException(null, E_NOTIMPL);

        public void GetScreenExt(uint vcView, UnmanagedMethods.RECT* prc)
            => throw new COMException(null, E_NOTIMPL);

        private void Trace(string message)
            => Logger.TryGet(LogEventLevel.Debug, LogArea.TextInput)?.Log(this, message);

        private void Trace<T0>(string template, T0 value)
            => Logger.TryGet(LogEventLevel.Debug, LogArea.TextInput)?.Log(this, template, value);
    }
}
