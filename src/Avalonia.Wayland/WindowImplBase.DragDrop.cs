using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Wayland.Clipboard;
using Avalonia.Wayland.Server.Persistent;
using Avalonia.Wayland.Server.Transient.Clipboard;

namespace Avalonia.Wayland;

partial class WindowBaseImpl
{
    partial class Sink
    {
        // DnD session state (Wayland-thread only; carried to UI thread via event args)
        private IDataTransfer? _currentDragDataTransfer;
        private DragDropEffects _currentDragSourceEffects;
        private WaylandDragMotionArgs? _pendingDragMotion;
        private WaylandOfferCookie? _currentDndCookie;

        /// <summary>
        /// Handles DnD-related dispatch in <see cref="DispatchInput"/>.
        /// Returns true if the event was DnD and was fully handled.
        /// </summary>
        protected bool HandleDragDropDispatch(RawInputEventArgs args)
        {
            // DnD motion debouncing: consume latest position under lock, create RawDragEvent
            if (args is WaylandDragMotionArgs dragMotion)
            {
                Point pos;
                RawInputModifiers mods;
                lock (dragMotion.SyncRoot)
                {
                    pos = dragMotion.Position;
                    mods = dragMotion.Modifiers;
                    dragMotion.Consumed = true;
                }

                var overEvent = new RawDragEvent(DragDropDevice.Instance, RawDragEventType.DragOver,
                    _inputRoot!, pos, dragMotion.DataTransfer, dragMotion.SourceEffects, mods);
                _p.Input?.Invoke(overEvent);

                var effects = overEvent.Effects;
                dragMotion.OfferCookie?.PostUpdateFeedback(effects);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Post-dispatch feedback for DnD enter/drop events.
        /// Called after <see cref="WindowBaseImpl.Input"/> has processed the event.
        /// Returns true if the event was DnD and feedback was sent.
        /// </summary>
        protected bool HandleDragDropPostDispatch(RawInputEventArgs args)
        {
            if (args is not WaylandRawDragEvent dragEvent)
                return false;

            // Route DnD feedback through the session cookie, which resolves
            // the correct data device on the Wayland thread.
            if (dragEvent.Type is RawDragEventType.DragEnter)
            {
                var effects = dragEvent.Effects;
                dragEvent.OfferCookie?.PostUpdateFeedback(effects);
            }
            else if (dragEvent.Type is RawDragEventType.Drop)
            {
                dragEvent.OfferCookie?.PostFinish();
            }

            return true;
        }

        void IWSurfaceEventSink.OnDragEnter(Point position, string[] mimeTypes, WaylandOfferCookie offerCookie,
            DragDropEffects sourceEffects, RawInputModifiers modifiers)
        {
            if (_inputRoot is null)
                return;

            _currentDndCookie = offerCookie;

            // Check for in-process drag source to avoid deadlock: if the offer contains
            // our sentinel MIME type, use the original IDataTransfer directly instead of
            // creating a pipe-based WaylandDataTransfer (whose reads would deadlock because
            // the source's send handler dispatches to the same UI thread).
            _currentDragDataTransfer = WaylandOutgoingTransfer.TryGetInProcessTransfer(mimeTypes)
                                       ?? new WaylandDataTransfer(mimeTypes, offerCookie);
            _currentDragSourceEffects = sourceEffects;
            _pendingDragMotion = null;

            ScheduleInput(new WaylandRawDragEvent(DragDropDevice.Instance, RawDragEventType.DragEnter,
                _inputRoot, position, _currentDragDataTransfer, sourceEffects, modifiers, offerCookie));
        }

        void IWSurfaceEventSink.OnDragMotion(Point position, RawInputModifiers modifiers)
        {
            var pending = _pendingDragMotion;
            if (pending != null)
            {
                lock (pending.SyncRoot)
                {
                    if (!pending.Consumed)
                    {
                        pending.Position = position;
                        pending.Modifiers = modifiers;
                        return;
                    }
                }
            }

            if (_inputRoot is null || _currentDragDataTransfer is null)
                return;

            var args = new WaylandDragMotionArgs(DragDropDevice.Instance, _inputRoot,
                position, _currentDragDataTransfer, _currentDragSourceEffects, modifiers, _currentDndCookie);
            _pendingDragMotion = args;
            _rawEventGrouper.HandleEvent(args);
        }

        void IWSurfaceEventSink.OnDragLeave()
        {
            if (_inputRoot is null || _currentDragDataTransfer is null)
                return;

            _pendingDragMotion = null;

            ScheduleInput(new RawDragEvent(DragDropDevice.Instance, RawDragEventType.DragLeave,
                _inputRoot, default, _currentDragDataTransfer, DragDropEffects.None, RawInputModifiers.None));

            _currentDragDataTransfer = null;
            _currentDndCookie = null;
        }

        void IWSurfaceEventSink.OnDrop(Point position, RawInputModifiers modifiers)
        {
            if (_inputRoot is null || _currentDragDataTransfer is null)
                return;

            _pendingDragMotion = null;

            ScheduleInput(new WaylandRawDragEvent(DragDropDevice.Instance, RawDragEventType.Drop,
                _inputRoot, position, _currentDragDataTransfer, _currentDragSourceEffects, modifiers, _currentDndCookie));

            _currentDragDataTransfer = null;
            _currentDndCookie = null;
        }
    }

    /// <summary>
    /// <see cref="RawDragEvent"/> subclass that carries the <see cref="WaylandOfferCookie"/>
    /// through the event queue so the UI thread can route DnD feedback without cross-thread
    /// field access.
    /// </summary>
    private class WaylandRawDragEvent(
        IDragDropDevice device, RawDragEventType type, IInputRoot root,
        Point location, IDataTransfer dataTransfer, DragDropEffects effects,
        RawInputModifiers modifiers, WaylandOfferCookie? offerCookie)
        : RawDragEvent(device, type, root, location, dataTransfer, effects, modifiers)
    {
        public WaylandOfferCookie? OfferCookie { get; } = offerCookie;
    }

    /// <summary>
    /// Specialized event args for DnD motion debouncing.
    /// <para>
    /// Ownership semantics: while a <see cref="WaylandDragMotionArgs"/> instance is in the dispatch
    /// queue, ownership belongs to the Wayland thread — it may update <see cref="Position"/> and
    /// <see cref="Modifiers"/> under <see cref="SyncRoot"/>. When the UI thread processes the event,
    /// it acquires the lock, reads the latest values, and sets <see cref="Consumed"/> to
    /// <c>true</c>. This atomically transfers ownership to the UI thread: subsequent Wayland-thread
    /// updates see <c>Consumed == true</c> and create a new args instance instead. Therefore the
    /// state is never truly shared — at any point exactly one thread owns the mutable fields.
    /// </para>
    /// </summary>
    private class WaylandDragMotionArgs(
        IDragDropDevice device, IInputRoot root,
        Point position, IDataTransfer dataTransfer,
        DragDropEffects sourceEffects, RawInputModifiers modifiers,
        WaylandOfferCookie? offerCookie)
        : RawInputEventArgs(device, 0, root)
    {
        public readonly object SyncRoot = new();
        public Point Position = position;
        public RawInputModifiers Modifiers = modifiers;
        public bool Consumed;
        public readonly IDataTransfer DataTransfer = dataTransfer;
        public readonly DragDropEffects SourceEffects = sourceEffects;
        public readonly WaylandOfferCookie? OfferCookie = offerCookie;
    }
}
