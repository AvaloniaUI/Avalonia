using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Input.TextInput;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.MicroCom;
using Avalonia.Win32.Interop;
using MicroCom.Runtime;
using static Avalonia.Win32.Input.Tsf.TsfConstants;

namespace Avalonia.Win32.Input.Tsf
{
    /// <summary>
    /// The TSF text store over <see cref="IStructuredTextInput"/>. The ACP document TSF sees is one
    /// linear scope of the client: the block around the selection. TSF reads and edits it through
    /// that scope's offsets, and the store moves to another scope, announced as a whole-document
    /// replacement, when the selection leaves the current one and no composition holds it there.
    /// Locks are granted synchronously on the UI thread (TSF drives the store from the thread that
    /// activated the thread manager); notifications raised while a lock is held are queued and
    /// flushed after release. A change TSF itself makes is reported back through the edit call's
    /// change record, and only what the document applied beyond it is announced as an app change.
    /// The store is also the context owner's composition sink: composition views map to
    /// <see cref="IStructuredTextInput.CompositionRange"/>, edits inside a composition update it in
    /// place, and the per-clause display attributes are read from the context's attribute property
    /// on each edit and reported as input decorations. Screen geometry is answered from the
    /// associated window's view.
    /// </summary>
    internal sealed unsafe class TsfTextStore : CallbackBase, ITextStoreACP2, ITfContextOwnerCompositionSink, ITfTextEditSink
    {
        private static readonly Guid s_iidTextStoreAcpSink = MicroComRuntime.GetGuidFor(typeof(ITextStoreACPSink));

        private const uint ViewCookie = 1;

        private IntPtr _hwnd;
        private WindowImpl? _window;
        private IStructuredTextInput? _client;
        private ILinearTextScope? _scope;
        private bool _scopeStale;
        private bool _secure;
        private ITextStoreACPSink? _sink;
        private uint _sinkMask;
        private uint _activeLockFlags;
        private bool _pendingAsyncLock;
        private uint _pendingAsyncLockFlags;
        private int _tsfCallDepth;
        private List<LinearTextChange>? _tsfEditChanges;
        private (int Anchor, int Active) _knownSelection;
        private readonly List<TS_TEXTCHANGE> _pendingTextChanges = new();
        private bool _pendingSelectionChange;
        private bool _pendingLayoutChange;
        private bool _pendingStatusChange;
        private ITfCompositionView? _compositionView;
        private ITfCategoryMgr? _categoryManager;
        private ITfDisplayAttributeMgr? _displayAttributeManager;
        private bool _displayAttributeManagersUnavailable;
        private readonly Dictionary<uint, ResolvedDisplayAttribute?> _displayAttributeCache = new();

        private readonly record struct ResolvedDisplayAttribute(
            TextInputDecorationKind Kind,
            Color? Foreground,
            Color? Background,
            TextInputUnderline Underline);

        /// <summary>The currently attached structured client, if any.</summary>
        public IStructuredTextInput? Client => _client;

        /// <summary>True while a composition view is held open by a text service.</summary>
        public bool HasActiveComposition => _compositionView is not null;

        // The document as TSF sees it: empty and read-only without a client, for a secure field
        // (a password), or for a read-only client.
        private bool IsReadOnlyDocument => _client is null || _secure || _client.IsReadOnly;

        private int Length => _secure ? 0 : _scope?.Length ?? 0;

        /// <summary>
        /// Tracks the window the store's view maps to; geometry answers convert between
        /// the window's top-level coordinate space and the screen through it.
        /// </summary>
        public void SetWindow(IntPtr hwnd, WindowImpl? window)
        {
            _hwnd = hwnd;
            _window = window;
        }

        /// <summary>
        /// Marks the focused field as secure (a password or other sensitive input). The text service
        /// then sees an empty read-only document, so it neither reads nor composes into the field, and
        /// typing reaches the control as plain key input.
        /// </summary>
        public void SetSecure(bool secure)
        {
            if (_secure == secure)
            {
                return;
            }

            var oldLength = Length;
            _secure = secure;
            AnnounceDocumentReplaced(oldLength);
        }

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

            // Any composition tracked against the old client is stale; TSF terminates its
            // side when the focus moves, and the old client keeps the text as it stands.
            _compositionView?.Dispose();
            _compositionView = null;

            var oldLength = Length;

            if (_client is not null)
            {
                if (_client.CompositionRange is not null)
                {
                    _client.CommitComposition(null);
                }

                _client.CaretPositionChanged -= OnClientCaretPositionChanged;
                _client.IsReadOnlyChanged -= OnClientReadOnlyChanged;
            }

            DetachScope();
            _client = client;
            _secure = false;

            if (_client is not null)
            {
                _client.CaretPositionChanged += OnClientCaretPositionChanged;
                _client.IsReadOnlyChanged += OnClientReadOnlyChanged;
                AttachScope(_client.GetLinearScope(_client.Selection.Active));
            }

            AnnounceDocumentReplaced(oldLength);
        }

        // The scope is the whole ACP document. Re-scoping announces a replacement of everything TSF
        // knew, which is how TSF learns that offsets now mean something else.
        private void AttachScope(ILinearTextScope scope)
        {
            DetachScope();
            _scope = scope;
            _scopeStale = false;
            _scope.TextChanged += OnScopeTextChanged;
            _scope.Invalidated += OnScopeInvalidated;
            _knownSelection = CurrentSelection();
        }

        private void DetachScope()
        {
            if (_scope is not null)
            {
                _scope.TextChanged -= OnScopeTextChanged;
                _scope.Invalidated -= OnScopeInvalidated;
                _scope = null;
            }
        }

        // Moves to the scope that holds the selection's active end, unless a composition pins the
        // current one: a composition never spans two scopes.
        private void RescopeIfNeeded()
        {
            if (_client is null || _compositionView is not null)
            {
                return;
            }

            var active = _client.Selection.Active;
            if (!_scopeStale && _scope is not null &&
                active.CompareTo(_scope.Range.Start) >= 0 && active.CompareTo(_scope.Range.End) <= 0)
            {
                return;
            }

            var oldLength = Length;
            AttachScope(_client.GetLinearScope(active));
            AnnounceDocumentReplaced(oldLength);
        }

        private void AnnounceDocumentReplaced(int oldLength)
        {
            QueueOrSend(() => NotifyStatusChange(), status: true);

            var newLength = Length;
            if (oldLength != 0 || newLength != 0)
            {
                var change = new TS_TEXTCHANGE { Start = 0, OldEnd = oldLength, NewEnd = newLength };
                if (_activeLockFlags != 0)
                {
                    _pendingTextChanges.Add(change);
                    _pendingSelectionChange = true;
                    _pendingLayoutChange = true;
                }
                else
                {
                    NotifyTextChange(change);
                    NotifySelectionChange();
                    NotifyLayoutChange();
                }
            }
        }

        private void QueueOrSend(Action send, bool status)
        {
            if (_activeLockFlags != 0)
            {
                if (status)
                {
                    _pendingStatusChange = true;
                }

                return;
            }

            send();
        }

        // Offsets in the current scope, clamped so a selection that reaches past the scope reads as
        // ending at its edge.
        private int Acp(ITextPointer position)
        {
            if (_scope is null || _secure)
            {
                return 0;
            }

            if (position.CompareTo(_scope.Range.Start) <= 0)
            {
                return 0;
            }

            if (position.CompareTo(_scope.Range.End) >= 0)
            {
                return _scope.Length;
            }

            return _scope.GetOffset(position);
        }

        private ITextRange ScopeRange(int start, int end) => _scope!.GetRange(start, end - start);

        private (int Anchor, int Active) CurrentSelection()
        {
            if (_client is null)
            {
                return (0, 0);
            }

            var selection = _client.Selection;
            return (Acp(selection.Anchor), Acp(selection.Active));
        }

        // App-originated notifications.

        private void OnScopeTextChanged(object? sender, LinearTextChange change)
        {
            if (_secure)
            {
                return;
            }

            // TSF's own edit: recorded so the edit call reports what the document really applied.
            if (_tsfEditChanges is not null)
            {
                _tsfEditChanges.Add(change);
                return;
            }

            var textChange = new TS_TEXTCHANGE
            {
                Start = change.Start,
                OldEnd = change.Start + change.OldLength,
                NewEnd = change.Start + change.NewLength,
            };

            if (_activeLockFlags != 0)
            {
                _pendingTextChanges.Add(textChange);
                _pendingLayoutChange = true;
                return;
            }

            NotifyTextChange(textChange);
            NotifyLayoutChange();
        }

        private void OnScopeInvalidated(object? sender, EventArgs e)
        {
            // The block behind the scope was merged, split or removed. Without a composition the
            // store moves on at once; with one it waits until the composition ends.
            _scopeStale = true;
            RescopeIfNeeded();
        }

        private void OnClientCaretPositionChanged(object? sender, EventArgs e)
        {
            RescopeIfNeeded();

            // A selection TSF set, or one it already read, is not news to it. Reporting it anyway
            // reads to a text service as the application moving the selection, which makes some
            // IMEs finalize the composition.
            var current = CurrentSelection();
            if (_tsfCallDepth > 0 || current == _knownSelection)
            {
                _knownSelection = current;
                return;
            }

            _knownSelection = current;

            if (_activeLockFlags != 0)
            {
                _pendingSelectionChange = true;
                _pendingLayoutChange = true;
                return;
            }

            NotifySelectionChange();
            NotifyLayoutChange();
        }

        private void OnClientReadOnlyChanged(object? sender, EventArgs e)
            => QueueOrSend(() => NotifyStatusChange(), status: true);

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
                Trace("OnTextChange failed: {Error}", DescribeError(exception));
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
                Trace("OnSelectionChange failed: {Error}", DescribeError(exception));
            }
        }

        private void NotifyLayoutChange()
        {
            if (_sink is null || (_sinkMask & TS_AS_LAYOUT_CHANGE) == 0)
            {
                return;
            }

            try
            {
                _sink.OnLayoutChange(TS_LC_CHANGE, ViewCookie);
            }
            catch (Exception exception)
            {
                Trace("OnLayoutChange failed: {Error}", DescribeError(exception));
            }
        }

        private void NotifyStatusChange()
        {
            if (_sink is null || (_sinkMask & TS_AS_STATUS_CHANGE) == 0)
            {
                return;
            }

            try
            {
                _sink.OnStatusChange(IsReadOnlyDocument ? TS_SD_READONLY : 0);
            }
            catch (Exception exception)
            {
                Trace("OnStatusChange failed: {Error}", DescribeError(exception));
            }
        }

        private void FlushPendingNotifications()
        {
            // Status first: whether the document is writable gates how much the rest
            // matters to a text service.
            if (_pendingStatusChange)
            {
                _pendingStatusChange = false;
                NotifyStatusChange();
            }

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
                _knownSelection = CurrentSelection();
                NotifySelectionChange();
            }

            if (_pendingLayoutChange)
            {
                _pendingLayoutChange = false;
                NotifyLayoutChange();
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

                // Queue the follow-up request; it is granted right after the current lock releases,
                // and the async session result is not reported. Requests queued in the meantime
                // merge, so a write request is never downgraded by a later read request.
                _pendingAsyncLockFlags = _pendingAsyncLock ? _pendingAsyncLockFlags | dwLockFlags : dwLockFlags;
                _pendingAsyncLock = true;
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
                GrantLock(_pendingAsyncLockFlags & ~TS_LF_SYNC);
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
            => IsReadOnlyDocument || _scope is null ? throw new COMException(null, TS_E_READONLY) : _client!;

        // Document access. Without a client the store reads as an empty document, so
        // requests in flight around focus transitions get consistent answers instead of
        // errors; mutation attempts report the read-only state.

        public void GetStatus(TS_STATUS* pdcs)
        {
            if (pdcs is null)
            {
                throw new COMException(null, E_INVALIDARG);
            }

            pdcs->DynamicFlags = IsReadOnlyDocument ? TS_SD_READONLY : 0;
            pdcs->StaticFlags = TS_SS_NOHIDDENTEXT;
        }

        public void GetEndACP(IntPtr pacp)
        {
            RequireLock(TS_LF_READ);
            *(int*)pacp = Length;
        }

        public void QueryInsert(int acpTestStart, int acpTestEnd, uint cch, IntPtr pacpResultStart, IntPtr pacpResultEnd)
        {
            if (acpTestStart < 0 || acpTestStart > acpTestEnd || acpTestEnd > Length)
            {
                throw new COMException(null, E_INVALIDARG);
            }

            // A read-only document accepts no insertion; TSF reads equal results as "nowhere".
            var accepted = !IsReadOnlyDocument;
            *(int*)pacpResultStart = acpTestStart;
            *(int*)pacpResultEnd = accepted ? acpTestEnd : acpTestStart;
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

            if (_client is null || _scope is null || _secure)
            {
                acp->Start = 0;
                acp->End = 0;
                acp->Style.ActiveSelectionEnd = TS_AE_NONE;
                *(uint*)pcFetched = 1;
                return;
            }

            var (anchor, active) = CurrentSelection();
            _knownSelection = (anchor, active);

            acp->Start = Math.Min(anchor, active);
            acp->End = Math.Max(anchor, active);
            acp->Style.ActiveSelectionEnd = anchor == active ? TS_AE_NONE : active < anchor ? TS_AE_START : TS_AE_END;

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

            if (acp->Start < 0 || acp->Start > acp->End || acp->End > Length)
            {
                throw new COMException(null, TS_E_INVALIDPOS);
            }

            // TS_AE_START puts the active end, and so the caret, at the start.
            var startIsActive = acp->Style.ActiveSelectionEnd == TS_AE_START;
            var start = _scope!.GetPosition(acp->Start, LogicalDirection.Forward);
            var end = _scope.GetPosition(acp->End, LogicalDirection.Backward);

            _tsfCallDepth++;
            try
            {
                client.Selection = startIsActive ? new DirectedTextRange(end, start) : new DirectedTextRange(start, end);
                _knownSelection = startIsActive ? (acp->End, acp->Start) : (acp->Start, acp->End);
            }
            finally
            {
                _tsfCallDepth--;
            }
        }

        public void GetText(int acpStart, int acpEnd, IntPtr pchPlain, uint cchPlainReq, IntPtr pcchPlainRet, IntPtr prgRunInfo, uint cRunInfoReq, IntPtr pcRunInfoRet, IntPtr pacpNext)
        {
            RequireLock(TS_LF_READ);
            var length = Length;

            if (acpEnd < 0)
            {
                acpEnd = length;
            }

            if (acpStart < 0 || acpStart > length || acpEnd < acpStart || acpEnd > length)
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
                var text = _scope!.GetText(acpStart, copied);
                fixed (char* source = text)
                {
                    Buffer.MemoryCopy(source, (void*)pchPlain, (long)cchPlainReq * sizeof(char), (long)copied * sizeof(char));
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

            if (acpStart < 0 || acpStart > acpEnd || acpEnd > Length)
            {
                throw new COMException(null, TS_E_INVALIDPOS);
            }

            var text = cch == 0 || pchText == IntPtr.Zero
                ? string.Empty
                : new string((char*)pchText, 0, (int)cch);

            var applied = ApplyTsfEdit(client, acpStart, acpEnd, text);

            if (pChange != IntPtr.Zero)
            {
                *(TS_TEXTCHANGE*)pChange = applied;
            }
        }

        public void InsertTextAtSelection(uint dwFlags, IntPtr pchText, uint cch, IntPtr pacpStart, IntPtr pacpEnd, IntPtr pChange)
        {
            var queryOnly = (dwFlags & TS_IAS_QUERYONLY) != 0;
            RequireLock(queryOnly ? TS_LF_READ : TS_LF_READWRITE);

            var client = RequireEditableClient();
            var (anchor, active) = CurrentSelection();
            var start = Math.Min(anchor, active);
            var end = Math.Max(anchor, active);

            var text = cch == 0 || pchText == IntPtr.Zero
                ? string.Empty
                : new string((char*)pchText, 0, (int)cch);

            int resultStart = start, resultEnd = end;

            if (!queryOnly)
            {
                var applied = ApplyTsfEdit(client, start, end, text);
                resultStart = applied.Start;
                resultEnd = applied.NewEnd;

                if (pChange != IntPtr.Zero)
                {
                    *(TS_TEXTCHANGE*)pChange = applied;
                }
            }

            // A query reports the span an insertion would replace; a real insertion the text it put there.
            if (pacpStart != IntPtr.Zero)
            {
                *(int*)pacpStart = resultStart;
            }

            if (pacpEnd != IntPtr.Zero)
            {
                *(int*)pacpEnd = resultEnd;
            }
        }

        // Applies an edit TSF asked for and returns the change the document actually made. An edit
        // inside the active composition updates the composition in place, so it stays one
        // composition and one undo step; anything else is committed input. When the document did
        // more than TSF asked (a handler rewriting the text in response), the first change answers
        // TSF and the rest are announced as application changes once the lock is released.
        private TS_TEXTCHANGE ApplyTsfEdit(IStructuredTextInput client, int start, int end, string text)
        {
            var changes = new List<LinearTextChange>();
            _tsfEditChanges = changes;
            _tsfCallDepth++;
            try
            {
                if (_compositionView is not null && client.CompositionRange is { } composition &&
                    Acp(composition.Start) is var compositionStart && Acp(composition.End) is var compositionEnd &&
                    start >= compositionStart && end <= compositionEnd)
                {
                    var current = _scope!.GetText(compositionStart, compositionEnd - compositionStart);
                    var updated = current.Remove(start - compositionStart, end - start)
                        .Insert(start - compositionStart, text);
                    client.SetCompositionText(updated, start - compositionStart + text.Length);
                }
                else
                {
                    client.ReplaceText(ScopeRange(start, end), text);
                }
            }
            finally
            {
                _tsfCallDepth--;
                _tsfEditChanges = null;
            }

            _knownSelection = CurrentSelection();

            if (changes.Count == 0)
            {
                return new TS_TEXTCHANGE { Start = start, OldEnd = start, NewEnd = start };
            }

            for (var i = 1; i < changes.Count; i++)
            {
                var extra = changes[i];
                _pendingTextChanges.Add(new TS_TEXTCHANGE
                {
                    Start = extra.Start,
                    OldEnd = extra.Start + extra.OldLength,
                    NewEnd = extra.Start + extra.NewLength,
                });
                _pendingLayoutChange = true;
            }

            var first = changes[0];
            return new TS_TEXTCHANGE
            {
                Start = first.Start,
                OldEnd = first.Start + first.OldLength,
                NewEnd = first.Start + first.NewLength,
            };
        }

        /// <summary>
        /// Mints a TSF range over the current selection through the advised sink's ACP
        /// services (no edit session needed); null when there is no sink, no client, or
        /// no selection to reconvert.
        /// </summary>
        public ITfRange? TryCreateSelectionRange()
        {
            if (_sink is null || _client is null || _scope is null || _secure)
            {
                return null;
            }

            var (anchor, active) = CurrentSelection();
            var start = Math.Min(anchor, active);
            var end = Math.Max(anchor, active);

            if (start == end)
            {
                return null;
            }

            ITextStoreACPServices? services = null;
            try
            {
                services = _sink.QueryInterface<ITextStoreACPServices>();
                return services.CreateRange(start, end);
            }
            catch (Exception exception)
            {
                Trace("Creating a selection range failed: {Error}", DescribeError(exception));
                return null;
            }
            finally
            {
                services?.Dispose();
            }
        }

        // The composition sink: TSF query-interfaces the context owner for it. The view's
        // range tracks IStructuredTextInput.CompositionRange, and the composition commits
        // through the structured client when TSF ends it (the text itself is already in
        // the document via the store's edit path).

        public void OnStartComposition(ITfCompositionView pComposition, IntPtr pfOk)
        {
            var accepted = !IsReadOnlyDocument && _scope is not null;

            if (pfOk != IntPtr.Zero)
            {
                *(int*)pfOk = accepted ? 1 : 0;
            }

            if (!accepted)
            {
                return;
            }

            _compositionView?.Dispose();
            _compositionView = pComposition.CloneReference();
            UpdateCompositionRange(pComposition);
            Trace("Composition started");
        }

        public void OnUpdateComposition(ITfCompositionView pComposition, ITfRange? pRangeNew)
        {
            if (_client is null || pRangeNew is null || _compositionView is null)
            {
                return;
            }

            UpdateCompositionRangeFromRange(pRangeNew);
        }

        public void OnEndComposition(ITfCompositionView pComposition)
        {
            _compositionView?.Dispose();
            _compositionView = null;

            if (_client is null)
            {
                return;
            }

            _client.CommitComposition(null);
            Trace("Composition ended");

            // A scope change that waited for the composition happens now.
            RescopeIfNeeded();
        }

        private void UpdateCompositionRange(ITfCompositionView view)
        {
            ITfRange? range = null;
            try
            {
                range = view.Range;
                UpdateCompositionRangeFromRange(range);
            }
            catch (COMException exception)
            {
                Trace("Composition range query failed: {Error}", DescribeError(exception));
            }
            finally
            {
                range?.Dispose();
            }
        }

        // Only a view that covers other text than the client's composition moves it; re-setting the
        // same extent would restart the composition and lose its undo step.
        private void UpdateCompositionRangeFromRange(ITfRange range)
        {
            if (_client is null || _scope is null || !TryGetRangeExtent(range, out var start, out var length))
            {
                return;
            }

            if (_client.CompositionRange is { } current &&
                Acp(current.Start) == start && Acp(current.End) == start + length)
            {
                return;
            }

            _tsfCallDepth++;
            try
            {
                _client.CompositionRange = _scope.GetRange(start, length);
            }
            finally
            {
                _tsfCallDepth--;
            }
        }

        private static bool TryGetRangeExtent(ITfRange range, out int start, out int length)
        {
            start = 0;
            length = 0;

            ITfRangeACP? acpRange = null;
            try
            {
                acpRange = range.QueryInterface<ITfRangeACP>();

                int anchor, count;
                acpRange.GetExtent(new IntPtr(&anchor), new IntPtr(&count));
                start = anchor;
                length = count;
                return true;
            }
            catch (COMException)
            {
                // A non-ACP range cannot appear over an ACP store; treat it as no extent.
                return false;
            }
            finally
            {
                acpRange?.Dispose();
            }
        }

        // The text edit sink, advised on the context through ITfSource: its read-only edit
        // cookie is the only channel to property values, so the per-clause display
        // attributes are re-read here after every TSF edit while a composition is active.

        public void OnEndEdit(ITfContext pic, uint ecReadOnly, ITfEditRecord pEditRecord)
        {
            if (_client is null || _compositionView is null || _scope is null)
            {
                return;
            }

            // A conversion can replace the composition text without moving its extent, so
            // OnUpdateComposition does not fire; re-anchor the range from the held view
            // after every edit in case the client's edit path disturbed it.
            UpdateCompositionRange(_compositionView);

            try
            {
                UpdateCompositionDecorations(pic, ecReadOnly);
            }
            catch (COMException exception)
            {
                Trace("Reading composition attributes failed: {Error}", DescribeError(exception));
            }
        }

        private void UpdateCompositionDecorations(ITfContext context, uint cookie)
        {
            var client = _client!;

            ITfRange? compositionRange = null;
            ITfReadOnlyProperty? trackedProperty = null;
            IEnumTfRanges? enumerator = null;
            try
            {
                compositionRange = _compositionView!.Range;

                // ITfContext.GetProperty answers E_FAIL for context-owner documents on
                // modern Windows; the channel that works (and the one the browsers use)
                // is a tracked-property aggregate over the attribute GUID.
                var attributeGuid = PropAttribute;
                var attributeGuidPtr = &attributeGuid;
                void* trackedPtr = null;
                context.TrackProperties(new IntPtr(&attributeGuidPtr), 1, IntPtr.Zero, 0, new IntPtr(&trackedPtr));
                if (trackedPtr == null)
                {
                    return;
                }

                trackedProperty = MicroComRuntime.CreateProxyFor<ITfReadOnlyProperty>(trackedPtr, true);

                void* enumeratorPtr = null;
                trackedProperty.EnumRanges(cookie, new IntPtr(&enumeratorPtr), compositionRange);
                if (enumeratorPtr == null)
                {
                    return;
                }

                enumerator = MicroComRuntime.CreateProxyFor<IEnumTfRanges>(enumeratorPtr, true);

                var decorations = new List<TextInputDecoration>();

                while (TryGetNextRange(enumerator, out var range))
                {
                    using (range)
                    {
                        if (!TryGetRangeExtent(range, out var start, out var length) || length <= 0)
                        {
                            continue;
                        }

                        var resolved = GetRangeDisplayAttribute(trackedProperty, cookie, range);
                        var clause = _scope!.GetRange(start, length);
                        decorations.Add(resolved is { } attribute
                            ? new TextInputDecoration(clause, attribute.Kind, attribute.Foreground, attribute.Background, attribute.Underline)
                            : new TextInputDecoration(clause, TextInputDecorationKind.Input));
                    }
                }

                client.SetInputDecorations(decorations);

                if (Logger.TryGet(LogEventLevel.Debug, LogArea.TextInput) is { } log)
                {
                    var summary = decorations.Count == 0
                        ? "none (client falls back to the whole-region default)"
                        : string.Join(", ", decorations.Select(d => $"[{Acp(d.Range.Start)}..{Acp(d.Range.End)}) {d.Kind}/{d.Underline}"));
                    log.Log(this, "Composition decorations: {Decorations}", summary);
                }
            }
            finally
            {
                enumerator?.Dispose();
                trackedProperty?.Dispose();
                compositionRange?.Dispose();
            }
        }

        private static bool TryGetNextRange(IEnumTfRanges enumerator, out ITfRange range)
        {
            range = null!;

            // The enumerator reports its end as S_FALSE - a success code the throwing
            // proxy would surface as an exception on every pass - so the Next slot is
            // called directly and the result stays data. Slot 4 = IUnknown (3) + Clone.
            void* rangePtr = null;
            uint fetched = 0;
            int hr;

            using (var lease = MicroComRuntime.LeaseNativePointerForCall(enumerator))
            {
                hr = ((delegate* unmanaged[Stdcall]<void*, uint, void**, uint*, int>)(*(void***)lease.Pointer)[4])(lease.Pointer, 1, &rangePtr, &fetched);
            }

            if (hr < 0 || fetched == 0 || rangePtr == null)
            {
                return false;
            }

            range = MicroComRuntime.CreateProxyFor<ITfRange>(rangePtr, true);
            return true;
        }

        private ResolvedDisplayAttribute? GetRangeDisplayAttribute(ITfReadOnlyProperty trackedProperty, uint cookie, ITfRange range)
        {
            var variant = default(TsfVariant);
            try
            {
                trackedProperty.GetValue(cookie, range, new IntPtr(&variant));

                // A tracked property aggregates its members: the value is an enumerator
                // of (guid, value) pairs, one per tracked GUID.
                if (variant.Vt != VT_UNKNOWN || variant.Data1 == IntPtr.Zero)
                {
                    return null;
                }

                using var unknown = MicroComRuntime.CreateProxyFor<IUnknown>((void*)variant.Data1, false);
                using var values = unknown.QueryInterface<IEnumTfPropertyValue>();

                var propertyValue = default(TF_PROPERTYVAL);
                try
                {
                    if (!TryGetNextPropertyValue(values, &propertyValue)
                        || propertyValue.GuidId != PropAttribute
                        || propertyValue.Value.Vt != VT_I4)
                    {
                        return null;
                    }

                    return ResolveDisplayAttribute((uint)propertyValue.Value.Data1.ToInt64());
                }
                finally
                {
                    VariantClear(new IntPtr(&propertyValue.Value));
                }
            }
            finally
            {
                VariantClear(new IntPtr(&variant));
            }
        }

        private static bool TryGetNextPropertyValue(IEnumTfPropertyValue enumerator, TF_PROPERTYVAL* value)
        {
            // Same S_FALSE-at-end contract as the range enumerator, so the Next slot is
            // called directly (slot 4 = IUnknown 3 + Clone).
            uint fetched = 0;
            int hr;

            using (var lease = MicroComRuntime.LeaseNativePointerForCall(enumerator))
            {
                hr = ((delegate* unmanaged[Stdcall]<void*, uint, void*, uint*, int>)(*(void***)lease.Pointer)[4])(lease.Pointer, 1, value, &fetched);
            }

            return hr >= 0 && fetched == 1;
        }

        // Display attribute atoms resolve through the category manager to the owning text
        // service's TF_DISPLAYATTRIBUTE; resolutions are cached per atom (atoms are stable
        // for the thread's lifetime).

        private ResolvedDisplayAttribute? ResolveDisplayAttribute(uint atom)
        {
            if (_displayAttributeCache.TryGetValue(atom, out var cached))
            {
                return cached;
            }

            ResolvedDisplayAttribute? resolved = null;
            try
            {
                EnsureDisplayAttributeManagers();

                if (_categoryManager is not null && _displayAttributeManager is not null)
                {
                    Guid attributeGuid;
                    _categoryManager.GetGUID(atom, new IntPtr(&attributeGuid));

                    void* infoPtr = null;
                    Guid ownerClsid;
                    _displayAttributeManager.GetDisplayAttributeInfo(&attributeGuid, new IntPtr(&infoPtr), new IntPtr(&ownerClsid));

                    if (infoPtr != null)
                    {
                        using var info = MicroComRuntime.CreateProxyFor<ITfDisplayAttributeInfo>(infoPtr, true);
                        var attribute = default(TF_DISPLAYATTRIBUTE);
                        info.GetAttributeInfo(&attribute);
                        resolved = MapDisplayAttribute(attribute);
                    }
                }
            }
            catch (COMException exception)
            {
                Trace("Display attribute resolution failed: {Error}", DescribeError(exception));
            }

            _displayAttributeCache[atom] = resolved;

            Trace(resolved is { } value
                ? $"Display attribute atom {atom}: {value.Kind}/{value.Underline}"
                : $"Display attribute atom {atom}: unresolved");

            return resolved;
        }

        private void EnsureDisplayAttributeManagers()
        {
            if (_displayAttributeManagersUnavailable || (_categoryManager is not null && _displayAttributeManager is not null))
            {
                return;
            }

            try
            {
                _categoryManager ??= UnmanagedMethods.CreateInstance<ITfCategoryMgr>(
                    in ClsidCategoryMgr, MicroComRuntime.GetGuidFor(typeof(ITfCategoryMgr)));
                _displayAttributeManager ??= UnmanagedMethods.CreateInstance<ITfDisplayAttributeMgr>(
                    in ClsidDisplayAttributeMgr, MicroComRuntime.GetGuidFor(typeof(ITfDisplayAttributeMgr)));
            }
            catch (COMException exception)
            {
                _displayAttributeManagersUnavailable = true;
                Trace("Display attribute managers unavailable: {Error}", DescribeError(exception));
            }
        }

        private static ResolvedDisplayAttribute MapDisplayAttribute(in TF_DISPLAYATTRIBUTE attribute)
        {
            var kind = attribute.Attribute switch
            {
                TF_ATTR_TARGET_CONVERTED => TextInputDecorationKind.ConvertedTarget,
                TF_ATTR_CONVERTED or TF_ATTR_FIXEDCONVERTED => TextInputDecorationKind.Converted,
                TF_ATTR_TARGET_NOTCONVERTED => TextInputDecorationKind.TargetNotConverted,
                TF_ATTR_INPUT_ERROR => TextInputDecorationKind.InputError,
                _ => TextInputDecorationKind.Input,
            };

            var underline = attribute.LineStyle switch
            {
                TF_LS_SOLID => attribute.BoldLine != 0 ? TextInputUnderline.Thick : TextInputUnderline.Single,
                TF_LS_DOT => TextInputUnderline.Dotted,
                TF_LS_DASH => TextInputUnderline.Dashed,
                TF_LS_SQUIGGLE => TextInputUnderline.Wavy,
                _ => TextInputUnderline.None,
            };

            return new ResolvedDisplayAttribute(kind, ToColor(attribute.Text), ToColor(attribute.Background), underline);
        }

        private static Color? ToColor(in TF_DA_COLOR color)
            => color.Type == TF_CT_COLORREF
                ? Color.FromRgb((byte)(color.Value & 0xFF), (byte)((color.Value >> 8) & 0xFF), (byte)((color.Value >> 16) & 0xFF))
                : null; // system palette indexes keep the theme's kind mapping

        [DllImport("oleaut32.dll")]
        private static extern int VariantClear(IntPtr pvarg);

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

        // Geometry: the single view is the associated window. Clients answer in top-level
        // device-independent coordinates (the contract's space), and the store converts to
        // and from physical screen coordinates through the window - the top-level origin
        // is the window's client-area origin, and the desktop scaling factor bridges the
        // unit systems.

        public void GetActiveView(IntPtr pvcView)
        {
            if (pvcView != IntPtr.Zero)
            {
                *(uint*)pvcView = ViewCookie;
            }
        }

        public void GetACPFromPoint(uint vcView, UnmanagedMethods.POINT* ptScreen, uint dwFlags, IntPtr pacp)
        {
            RequireView(vcView);
            RequireLock(TS_LF_READ);

            if (ptScreen is null || pacp == IntPtr.Zero)
            {
                throw new COMException(null, E_INVALIDARG);
            }

            if (_client is null || _scope is null || _secure)
            {
                *(int*)pacp = 0;
                return;
            }

            var point = ToTopLevelPoint(*ptScreen);

            // Without GXFPF_ROUND_NEAREST the answer is the character under the point, not the
            // nearest boundary; without GXFPF_NEAREST a point that is not over text is an error.
            if ((dwFlags & GXFPF_ROUND_NEAREST) == 0 && _client.GetCharacterRangeAtPoint(point) is { } character)
            {
                if ((dwFlags & GXFPF_NEAREST) == 0 && !_client.GetFirstRectForRange(character).Contains(point))
                {
                    throw new COMException(null, TS_E_INVALIDPOINT);
                }

                *(int*)pacp = Acp(character.Start);
                return;
            }

            var position = _client.GetClosestPosition(point)
                ?? throw new COMException(null, TS_E_INVALIDPOINT);

            *(int*)pacp = Acp(position);
        }

        public void GetTextExt(uint vcView, int acpStart, int acpEnd, UnmanagedMethods.RECT* prc, IntPtr pfClipped)
        {
            RequireView(vcView);
            RequireLock(TS_LF_READ);

            if (prc is null)
            {
                throw new COMException(null, E_INVALIDARG);
            }

            var client = _client is not null && _scope is not null && !_secure
                ? _client
                : throw new COMException(null, TS_E_NOLAYOUT);
            var length = Length;

            if (acpStart < 0 || acpEnd < acpStart || acpStart > length)
            {
                throw new COMException(null, TS_E_INVALIDPOS);
            }

            acpEnd = Math.Min(acpEnd, length);

            Rect rect;
            if (acpStart == acpEnd)
            {
                rect = client.GetCaretRect(_scope!.GetPosition(acpStart, LogicalDirection.Forward));
            }
            else
            {
                rect = default;
                foreach (var lineRect in client.GetSelectionRects(ScopeRange(acpStart, acpEnd)))
                {
                    rect = rect == default ? lineRect : rect.Union(lineRect);
                }

                if (rect == default)
                {
                    rect = client.GetFirstRectForRange(ScopeRange(acpStart, acpEnd));
                }
            }

            if (rect == default)
            {
                // The range has no realized layout yet; the sink retries after
                // OnLayoutChange.
                throw new COMException(null, TS_E_NOLAYOUT);
            }

            *prc = ToScreenRect(rect);

            if (pfClipped != IntPtr.Zero)
            {
                *(int*)pfClipped = 0;
            }
        }

        public void GetScreenExt(uint vcView, UnmanagedMethods.RECT* prc)
        {
            RequireView(vcView);

            if (prc is null)
            {
                throw new COMException(null, E_INVALIDARG);
            }

            *prc = default;

            if (_hwnd == IntPtr.Zero || !UnmanagedMethods.GetClientRect(_hwnd, out var clientRect))
            {
                return;
            }

            var origin = default(UnmanagedMethods.POINT);
            UnmanagedMethods.ClientToScreen(_hwnd, ref origin);

            prc->left = origin.X + clientRect.left;
            prc->top = origin.Y + clientRect.top;
            prc->right = origin.X + clientRect.right;
            prc->bottom = origin.Y + clientRect.bottom;
        }

        // GetACPFromPoint flags (msctf.h).
        private const uint GXFPF_ROUND_NEAREST = 0x1;
        private const uint GXFPF_NEAREST = 0x2;

        private static void RequireView(uint vcView)
        {
            if (vcView != ViewCookie)
            {
                throw new COMException(null, E_INVALIDARG);
            }
        }

        private UnmanagedMethods.RECT ToScreenRect(Rect topLevelRect)
        {
            var scaling = _window?.DesktopScaling ?? 1.0;
            var origin = default(UnmanagedMethods.POINT);

            if (_hwnd != IntPtr.Zero)
            {
                UnmanagedMethods.ClientToScreen(_hwnd, ref origin);
            }

            return new UnmanagedMethods.RECT
            {
                left = origin.X + (int)Math.Floor(topLevelRect.X * scaling),
                top = origin.Y + (int)Math.Floor(topLevelRect.Y * scaling),
                right = origin.X + (int)Math.Ceiling(topLevelRect.Right * scaling),
                bottom = origin.Y + (int)Math.Ceiling(topLevelRect.Bottom * scaling),
            };
        }

        private Point ToTopLevelPoint(UnmanagedMethods.POINT screenPoint)
        {
            var point = screenPoint;

            if (_hwnd != IntPtr.Zero)
            {
                UnmanagedMethods.ScreenToClient(_hwnd, ref point);
            }

            var scaling = _window?.DesktopScaling ?? 1.0;
            return new Point(point.X / scaling, point.Y / scaling);
        }

        protected override void Destroyed()
        {
            DetachScope();
            _compositionView?.Dispose();
            _compositionView = null;
            _categoryManager?.Dispose();
            _categoryManager = null;
            _displayAttributeManager?.Dispose();
            _displayAttributeManager = null;
            _sink?.Dispose();
            _sink = null;
        }

        private static string DescribeError(Exception exception)
            => exception is COMException com ? $"0x{com.HResult:X8}" : exception.Message;

        private void Trace(string message)
            => Logger.TryGet(LogEventLevel.Debug, LogArea.TextInput)?.Log(this, message);

        private void Trace<T0>(string template, T0 value)
            => Logger.TryGet(LogEventLevel.Debug, LogArea.TextInput)?.Log(this, template, value);
    }
}
