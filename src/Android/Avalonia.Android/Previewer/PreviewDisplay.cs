using System;
using Android.Content;
using Android.Hardware.Display;
using Android.Util;
using Android.Views;

namespace Avalonia.Android.Previewer
{
    internal class PreviewDisplay : IDisposable
    {
        private const int VIRTUAL_DISPLAY_FLAG_DESTROY_CONTENT_ON_REMOVAL = 1 << 8;
        private const int VIRTUAL_DISPLAY_FLAG_SHOULD_SHOW_SYSTEM_DECORATIONS = 1 << 9;
        private const int VIRTUAL_DISPLAY_FLAG_OWN_FOCUS = 1 << 14;
        private const int VIRTUAL_DISPLAY_FLAG_DEVICE_DISPLAY_GROUP = 1 << 15;

        public static PreviewDisplay? Instance {  get; private set; }

        public static PreviewDisplay? GetOrCreateDisplay(DisplayMetrics metrics, Context context) => Instance ??= new PreviewDisplay(metrics, context);

        private VirtualDisplay? _virtualDisplay;
        private readonly DisplayMetrics _metrics;
        private readonly Context _context;

        private PreviewDisplay(DisplayMetrics metrics, Context context)
        {
            _metrics = metrics;
            _context = context;
        }

        public int DisplayId => _virtualDisplay?.Display?.DisplayId ?? 0;

        public Display? Display => _virtualDisplay?.Display;

        internal PreviewSurface? Surface { get; private set; }

        public void Dispose()
        {
            StopDisplay();
        }

        public void StartDisplay()
        {
            Surface = new PreviewSurface(_metrics.WidthPixels, _metrics.HeightPixels);
            var displayManager = _context.GetSystemService(Context.DisplayService) as DisplayManager;

            var flags = VirtualDisplayFlags.Public |
                VirtualDisplayFlags.Presentation |
                VirtualDisplayFlags.OwnContentOnly |
                (VirtualDisplayFlags)(VIRTUAL_DISPLAY_FLAG_DESTROY_CONTENT_ON_REMOVAL
                | VIRTUAL_DISPLAY_FLAG_SHOULD_SHOW_SYSTEM_DECORATIONS);
            if (OperatingSystem.IsAndroidVersionAtLeast(34))
            {
                flags |= (VirtualDisplayFlags)(
                    VIRTUAL_DISPLAY_FLAG_OWN_FOCUS
                            | VIRTUAL_DISPLAY_FLAG_DEVICE_DISPLAY_GROUP
                    );
            }
            if (displayManager != null)
            {
                _virtualDisplay = displayManager.CreateVirtualDisplay($"{_context.PackageName}-virtualDisplay",
                    (int)Surface.Width,
                    (int)Surface.Height,
                    (int)_metrics.DensityDpi,
                    Surface.Surface,
                    flags);
            }
        }

        public void StopDisplay()
        {
            _virtualDisplay?.Release();
            _virtualDisplay = null;
        }
    }
}
