using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Input.GestureRecognizers;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    internal enum CaptureSource
    {
        Explicit,
        Implicit,
        Platform
    }

    public class Pointer : IPointer, IDisposable
    {
        private static int s_NextFreePointerId = 1000;
        public static int GetNextFreeId() => s_NextFreePointerId++;
        
        private bool _disposed;

        public Pointer(int id, PointerType type, bool isPrimary)
        {
            Id = id;
            Type = type;
            IsPrimary = isPrimary;
        }

        public int Id { get; }

        static IInputElement? FindCommonParent(IInputElement? control1, IInputElement? control2)
        {
            if (control1 is not Visual c1 || control2 is not Visual c2)
                return null;
            var seen = new HashSet<IInputElement>(c1.GetSelfAndVisualAncestors().OfType<IInputElement>());
            return c2.GetSelfAndVisualAncestors().OfType<IInputElement>().FirstOrDefault(seen.Contains);
        }

        protected virtual void PlatformCapture(IInputElement? element)
        {

        }

        internal void PlatformCaptureLost() => CaptureLost(CaptureSource.Platform);

        /// <summary>
        /// Ends every capture the pointer holds, on the element and on a gesture recognizer.
        /// </summary>
        internal void CaptureLost(CaptureSource source)
        {
            if (_disposed)
                return;

            CaptureLostCore(source);
        }

        private void CaptureLostCore(CaptureSource source)
        {
            CaptureCore(null, null, source);
            IsGestureRecognitionSkipped = false;
        }

        public void Capture(IInputElement? control)
        {
            Capture(control, CaptureSource.Explicit);
        }

        private IInputElement? EffectiveCapturer => this.CapturedGestureRecognizer?.Target ?? Captured;

        internal void Capture(IInputElement? control, CaptureSource source)
        {
            if (_disposed)
            {
                Debug.Assert(control is null, "Capturing a pointer that no longer exists.");
                return;
            }

            CaptureCore(control, null, source);
        }

        private void CaptureCore(
            IInputElement? control,
            GestureRecognizer? gestureRecognizer,
            CaptureSource source)
        {
            var oldCapture = Captured;
            var oldGestureRecognizer = CapturedGestureRecognizer;
            var oldSource = CaptureSource;
            var oldEffectiveCapturer = EffectiveCapturer;

            // If a handler marks Implicit capture as handled, we still want them to have another chance if the element is captured explicitly.
            if (oldCapture == control && oldGestureRecognizer == gestureRecognizer && oldSource == source)
                return;

            var oldVisual = oldCapture as Visual;
            var newVisual = control as Visual;

            IInputElement? commonParent = null;
            if (oldVisual != null || newVisual != null)
            {
                commonParent = FindCommonParent(control, oldCapture);
                var visual = oldVisual ?? newVisual!; // We want the capture to be cancellable even if there is no currently captured element.
                foreach (var notifyTarget in visual.GetSelfAndVisualAncestors().OfType<IInputElement>())
                {
                    var args = new PointerCaptureChangingEventArgs(notifyTarget, this, control, source);
                    notifyTarget.RaiseEvent(args);
                    if (args.Handled)
                        return;
                    if (notifyTarget == commonParent)
                        break;
                }
            }

            if (oldVisual != null)
                oldVisual.DetachedFromVisualTree -= OnCaptureDetached;

            if (oldGestureRecognizer != gestureRecognizer)
                oldGestureRecognizer?.PointerCaptureLostInternal(this);

            Captured = control;
            CapturedGestureRecognizer = gestureRecognizer;
            CaptureSource = source;

            // However, we still want to notify the platform only if the captured element actually changed.
            if (oldEffectiveCapturer != EffectiveCapturer && source != CaptureSource.Platform)
                PlatformCapture(EffectiveCapturer);

            if (oldVisual != null)
                foreach (var notifyTarget in oldVisual.GetSelfAndVisualAncestors().OfType<IInputElement>())
                {
                    if (notifyTarget == commonParent)
                        break;
                    notifyTarget.RaiseEvent(new PointerCaptureLostEventArgs(notifyTarget, this));
                }

            if (newVisual != null)
                newVisual.DetachedFromVisualTree += OnCaptureDetached;

            if (Captured == null && CapturedGestureRecognizer == null)
            {
                IsGestureRecognitionSkipped = false;
            }

            // Update the pointer-over + cursor immediately following the capture change
            if (Type != PointerType.Touch)
            {
                var oldInputRoot = oldVisual?.PresentationSource?.InputRoot;
                var newInputRoot = newVisual?.PresentationSource?.InputRoot;

                oldInputRoot?.PointerOverInvalidated();

                if (oldInputRoot != newInputRoot)
                    newInputRoot?.PointerOverInvalidated();
            }
        }

        static IInputElement? GetNextCapture(Visual? parent)
        {
            return parent as IInputElement ?? parent.FindAncestorOfType<IInputElement>();
        }

        private void OnCaptureDetached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            Capture(GetNextCapture(e.AttachmentPoint));
        }


        public IInputElement? Captured { get; private set; }

        public PointerType Type { get; }

        public bool IsPrimary { get; }

        /// <summary>
        /// Gets the gesture recognizer that is currently capturing by the pointer, if any.
        /// </summary>
        internal GestureRecognizer? CapturedGestureRecognizer { get; private set; }

        public bool IsGestureRecognitionSkipped { get; set; }

        internal CaptureSource CaptureSource { get; private set; } = CaptureSource.Platform;

        public void Dispose()
        {
            if (_disposed)
                return;

            // Mark the pointer gone first, so a capture lost handler can't capture it again.
            // It no longer exists, so the platform is the only source the release can come from.
            _disposed = true;

            CaptureLostCore(CaptureSource.Platform);
        }

        /// <summary>
        /// Captures pointer input to the specified gesture recognizer.
        /// </summary>
        /// <param name="gestureRecognizer">The gesture recognizer.</param>
        internal void CaptureGestureRecognizer(GestureRecognizer? gestureRecognizer)
        {
            if (_disposed)
            {
                Debug.Assert(gestureRecognizer is null, "Capturing a pointer that no longer exists to a gesture recognizer.");
                return;
            }

            CaptureCore(null, gestureRecognizer, CaptureSource.Explicit);
        }
    }
}
