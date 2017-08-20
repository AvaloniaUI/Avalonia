using System;
using Avalonia.Controls.Embedding;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Avalonia.Win32.Interop.Wpf
{
    class WpfMouseDevice : MouseDevice
    {
        private readonly WpfTopLevelImpl _impl;

        public WpfMouseDevice(WpfTopLevelImpl impl)
        {
            _impl = impl;
        }

        public override void Capture(IInputElement control)
        {
            if (control == null)
            {
                System.Windows.Input.Mouse.Capture(null);
            }
            else if ((control.GetVisualRoot() as EmbeddableControlRoot)?.PlatformImpl != _impl)
                throw new ArgumentException("Visual belongs to unknown toplevel");
            else
                System.Windows.Input.Mouse.Capture(_impl);
            base.Capture(control);
        }
    }
}