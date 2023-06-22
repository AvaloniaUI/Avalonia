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
        private IWindowIconImpl? _icon;

        private IPlatformSettings _platformSettings;

        public TrayIconImpl(IAvaloniaNativeFactory factory)
        {
            _native = factory.CreateTrayIcon();

            MenuExporter = new AvaloniaNativeMenuExporter(_native, factory);

            _platformSettings = AvaloniaLocator.Current.GetRequiredService<IPlatformSettings>();

            _platformSettings.ThemeVariantChanged += OnThemeVariantChanged;
        }

        private void OnThemeVariantChanged(object? sender, EventArgs e)
        {
            if (_icon is IThemeVariantWindowIconImpl)
            {
                RefreshIcon();
            }
        }

        public Action? OnClicked { get; set; }

        public void Dispose()
        {
            _native.Dispose();
            _platformSettings.ThemeVariantChanged -= OnThemeVariantChanged;
        }

        public void SetIcon(IWindowIconImpl? icon)
        {
            _icon = icon;
            RefreshIcon();
        }

        private unsafe void RefreshIcon()
        {
            if (_icon switch
            {
                IThemeVariantWindowIconImpl variantIcons => _platformSettings.ThemeVariant switch
                {
                    PlatformThemeVariant.Light => variantIcons.Light,
                    PlatformThemeVariant.Dark => variantIcons.Dark,
                    _ => throw new NotImplementedException()
                },
                _ => _icon,
            } is not { } icon)
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
            _native.SetToolTipText(text);
        }

        public void SetIsVisible(bool visible)
        {
            _native.SetIsVisible(visible.AsComBool());
        }

        public INativeMenuExporter? MenuExporter { get; }
    }
}
