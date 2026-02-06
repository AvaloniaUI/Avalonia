using System;
using System.Collections.Generic;
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

        internal void PlatformCaptureLost()
        {
            if (Captured != null)
                Capture(null, CaptureSource.Platform);
        }

        public void Capture(IInputElement? control)
        {
            Capture(control, CaptureSource.Explicit);
        }

        internal void Capture(IInputElement? control, CaptureSource source)
        {
            var oldCapture = Captured;
            var oldSource = CaptureSource;

            // If a handler marks Implicit capture as handled, we still want them to have another chance if the element is captured explicitly.
            if (oldCapture == control && oldSource == source)
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
            Captured = control;
            CaptureSource = source;

            // However, we still want to notify the platform only if the captured element actually changed.
            if (oldCapture != control && source != CaptureSource.Platform)
                PlatformCapture(control);

            if (oldVisual != null)
                foreach (var notifyTarget in oldVisual.GetSelfAndVisualAncestors().OfType<IInputElement>())
                {
                    if (notifyTarget == commonParent)
                        break;
                    notifyTarget.RaiseEvent(new PointerCaptureLostEventArgs(notifyTarget, this));
                }

            if (newVisual != null)
                newVisual.DetachedFromVisualTree += OnCaptureDetached;

            if (Captured != null)
                CaptureGestureRecognizer(null);

            if(Captured == null && CapturedGestureRecognizer == null)
            {
                IsGestureRecognitionSkipped = false;
            }
        }

        static IInputElement? GetNextCapture(Visual? parent)
        {
            return parent as IInputElement ?? parent.FindAncestorOfType<IInputElement>();
        }

        private void OnCaptureDetached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            Capture(GetNextCapture(e.Parent));
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
            if (Captured != null)
            {
                Capture(null);
            }
        }

        /// <summary>
        /// Captures pointer input to the specified gesture recognizer.
        /// </summary>
        /// <param name="gestureRecognizer">The gesture recognizer.</param>
        internal void CaptureGestureRecognizer(GestureRecognizer? gestureRecognizer)
        {
            if (CapturedGestureRecognizer != gestureRecognizer)
            {
                CapturedGestureRecognizer?.PointerCaptureLostInternal(this);
            }

            CapturedGestureRecognizer = gestureRecognizer;

            if (gestureRecognizer != null)
            {
                Capture(null);
            }
        }
    }
}
