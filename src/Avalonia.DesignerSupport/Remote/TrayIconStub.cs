using System;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Platform;

namespace Avalonia.DesignerSupport.Remote
{
    class TrayIconStub : ITrayIconImpl
    {
        public Action Clicked { get; set; }
        public Action DoubleClicked { get; set; }
        public Action RightClicked { get; set; }

        public INativeMenuExporter MenuExporter => null;

        public Action OnClicked { get; set; }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void SetIcon(IWindowIconImpl icon)
        {   
        }

        public void SetIsVisible(bool visible)
        {   
        }

        public void SetMenu(NativeMenu menu)
        {
            throw new NotImplementedException();
        }

        public void SetToolTipText(string text)
        {
        }
    }
}
