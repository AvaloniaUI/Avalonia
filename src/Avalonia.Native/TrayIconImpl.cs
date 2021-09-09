using System;
using System.IO;
using Avalonia.Controls.Platform;
using Avalonia.Native.Interop;
using Avalonia.Platform;

namespace Avalonia.Native
{
    class TrayIconEvents : CallbackBase, IAvnTrayIconEvents
    {
        private TrayIconImpl _parent;

        public TrayIconEvents (TrayIconImpl parent)
        {
            _parent = parent;
        }

        public void Clicked()
        {   
        }

        public void DoubleClicked()
        {
        }
    }
    
    internal class TrayIconImpl : ITrayIconImpl
    {
        private readonly IAvnTrayIcon _native;
        
        public TrayIconImpl(IAvaloniaNativeFactory factory)
        {
            _native = factory.CreateTrayIcon(new TrayIconEvents(this));
        }
        
        public void Dispose()
        {
            
        }

        public unsafe void SetIcon(IWindowIconImpl? icon)
        {
            if(icon is null)
            {
                _native.SetIcon(null, IntPtr.Zero);
            }
            else
            {
                using (var ms = new MemoryStream())
                {
                    icon.Save(ms);

                    var imageData = ms.ToArray();

                    fixed(void* ptr = imageData)
                    {
                        _native.SetIcon(ptr, new IntPtr(imageData.Length));
                    }
                }
            }
        }

        public void SetToolTipText(string? text)
        {
            // NOP
        }

        public void SetIsVisible(bool visible)
        {
            
        }

        public INativeMenuExporter? MenuExporter { get; }
    }
}
