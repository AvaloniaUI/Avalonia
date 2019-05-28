using System;
using System.Linq;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public class Pointer : IPointer, IDisposable
    {
        public Pointer(int id, PointerType type, bool isPrimary, IInputElement implicitlyCaptured)
        {
            Id = id;
            Type = type;
            IsPrimary = isPrimary;
            ImplicitlyCaptured = implicitlyCaptured;
            if (ImplicitlyCaptured != null)
                ImplicitlyCaptured.DetachedFromVisualTree += OnImplicitCaptureDetached;
        }

        public int Id { get; }

        public void Capture(IInputElement control)
        {
            if (Captured != null)
                Captured.DetachedFromVisualTree -= OnCaptureDetached;
            Captured = control;
            if (Captured != null)
                Captured.DetachedFromVisualTree += OnCaptureDetached;
        }

        IInputElement GetNextCapture(IVisual parent) =>
            parent as IInputElement ?? parent.GetVisualAncestors().OfType<IInputElement>().FirstOrDefault();
        
        private void OnCaptureDetached(object sender, VisualTreeAttachmentEventArgs e)
        {
            Capture(GetNextCapture(e.Parent));
        }

        private void OnImplicitCaptureDetached(object sender, VisualTreeAttachmentEventArgs e)
        {
            ImplicitlyCaptured.DetachedFromVisualTree -= OnImplicitCaptureDetached;
            ImplicitlyCaptured = GetNextCapture(e.Parent);
            if (ImplicitlyCaptured != null)
                ImplicitlyCaptured.DetachedFromVisualTree += OnImplicitCaptureDetached;
        }

        public IInputElement Captured { get; private set; }
        public IInputElement ImplicitlyCaptured { get; private set; }
        public IInputElement GetEffectiveCapture() => Captured ?? ImplicitlyCaptured;
            
        public PointerType Type { get; }
        public bool IsPrimary { get; }
        public void Dispose()
        {
            if (ImplicitlyCaptured != null)
                ImplicitlyCaptured.DetachedFromVisualTree -= OnImplicitCaptureDetached;
            if (Captured != null)
                Captured.DetachedFromVisualTree -= OnCaptureDetached;
        }
    }
}