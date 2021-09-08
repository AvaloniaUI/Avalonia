using System;
using Avalonia.Platform;

namespace Avalonia.DesignerSupport.Remote
{
    class TrayIconStub : ITrayIconImpl
    {
        public Action Clicked { get; set; }
        public Action DoubleClicked { get; set; }
        public Action RightClicked { get; set; }

        public void SetIcon(IWindowIconImpl icon)
        {   
        }

        public void SetIsVisible(bool visible)
        {   
        }

        public void SetToolTipText(string text)
        {
        }
    }
}
