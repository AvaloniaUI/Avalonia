using System;
using System.IO;
using Avalonia.Controls.Platform;
using Avalonia.Native.Interop;
using Avalonia.Platform;

#nullable enable

namespace Avalonia.Native
{
    internal class TrayIconImpl : ITrayIconImpl
    {
        private readonly IAvnTrayIcon _native;

        public TrayIconImpl(IAvaloniaNativeFactory factory)
        {
            _native = factory.CreateTrayIcon();

            MenuExporter = new AvaloniaNativeMenuExporter(_native, factory);
        }

        public Action? OnClicked { get; set; }

        public void Dispose()
        {
            _native.Dispose();
        }

        public unsafe void SetIcon(IWindowIconImpl? icon)
        {
            if (icon is null)
            {
                _native.SetIcon(null, IntPtr.Zero);
            }
            else
            {
                using (var ms = new MemoryStream())
                {
                    icon.Save(ms);

                    var imageData = ms.ToArray();

                    fixed (void* ptr = imageData)
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
            _native.SetIsVisible(visible.AsComBool());
        }

        public INativeMenuExporter? MenuExporter { get; }
    }
}
