using System;
using System.Collections.Generic;
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
            
            // Walk the ancestor chain directly without LINQ allocation
            // First, mark all ancestors of c1
            var seen = new HashSet<Visual>();
            for (var current = c1; current != null; current = current.VisualParent)
            {
                if (current is IInputElement)
                    seen.Add(current);
            }
            
            // Then find first common ancestor in c2's chain
            for (var current = c2; current != null; current = current.VisualParent)
            {
                if (current is IInputElement element && seen.Contains(current))
                    return element;
            }
            
            return null;
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
            if (oldCapture == control)
                return;

            var oldVisual = oldCapture as Visual;

            IInputElement? commonParent = null;
            if (oldVisual != null)
            {
                commonParent = FindCommonParent(control, oldCapture);
                // Walk ancestor chain directly instead of OfType<IInputElement>() LINQ allocation
                for (Visual? current = oldVisual; current != null; current = current.VisualParent)
                {
                    if (current is not IInputElement notifyTarget)
                        continue;
                    if (notifyTarget == commonParent)
                        break;
                    var args = new PointerCaptureChangingEventArgs(notifyTarget, this, control, source);
                    notifyTarget.RaiseEvent(args);
                    if (args.Handled)
                        return;
                }
            }

            if (oldVisual != null)
                oldVisual.DetachedFromVisualTree -= OnCaptureDetached;
            Captured = control;

            if (source != CaptureSource.Platform)
                PlatformCapture(control);

            if (oldVisual != null)
            {
                // Walk ancestor chain directly instead of OfType<IInputElement>() LINQ allocation
                for (Visual? current = oldVisual; current != null; current = current.VisualParent)
                {
                    if (current is not IInputElement notifyTarget)
                        continue;
                    if (notifyTarget == commonParent)
                        break;
                    notifyTarget.RaiseEvent(new PointerCaptureLostEventArgs(notifyTarget, this));
                }
            }

            if (Captured is Visual newVisual)
                newVisual.DetachedFromVisualTree += OnCaptureDetached;

            if (Captured != null)
                CaptureGestureRecognizer(null);

            if(Captured == null && CapturedGestureRecognizer == null)
            {
                IsGestureRecognitionSkipped = false;
            }
        }

        static IInputElement? GetNextCapture(Visual parent)
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
