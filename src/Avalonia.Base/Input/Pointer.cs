using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
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

        IInputElement? FindCommonParent(IInputElement? control1, IInputElement? control2)
        {
            if (control1 is not Visual c1 || control2 is not Visual c2)
                return null;
            var seen = new HashSet<IInputElement>(c1.GetSelfAndVisualAncestors().OfType<IInputElement>());
            return c2.GetSelfAndVisualAncestors().OfType<IInputElement>().FirstOrDefault(seen.Contains);
        }

        protected virtual void PlatformCapture(IInputElement? element)
        {
            
        }
        
        public void Capture(IInputElement? control)
        {
            if (Captured is Visual v1)
                v1.DetachedFromVisualTree -= OnCaptureDetached;
            var oldCapture = Captured;
            Captured = control;
            PlatformCapture(control);
            if (oldCapture is Visual v2)
            {
                var commonParent = FindCommonParent(control, oldCapture);
                foreach (var notifyTarget in v2.GetSelfAndVisualAncestors().OfType<IInputElement>())
                {
                    if (notifyTarget == commonParent)
                        break;
                    notifyTarget.RaiseEvent(new PointerCaptureLostEventArgs(notifyTarget, this));
                }
            }

            if (Captured is Visual v3)
                v3.DetachedFromVisualTree += OnCaptureDetached;
        }

        IInputElement? GetNextCapture(Visual parent)
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
        public void Dispose() => Capture(null);
    }
}
