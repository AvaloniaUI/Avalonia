using System;
using System.Collections.Generic;
using Avalonia.Native.Interop;
using Avalonia.Platform;
using MicroCom.Runtime;

namespace Avalonia.Native
{
    internal sealed class AvnScreen(uint displayId)
        : PlatformScreen(new PlatformHandle(new IntPtr(displayId), "CGDirectDisplayID"))
    {
        public unsafe void Refresh(IAvnScreens native)
        {
            void* localizedName = null;
            var screen = native.GetScreen(displayId, &localizedName);

            IsPrimary = screen.IsPrimary.FromComBool();
            Scaling = screen.Scaling;
            Bounds = screen.Bounds.ToAvaloniaPixelRect();
            WorkingArea = screen.WorkingArea.ToAvaloniaPixelRect();
            CurrentOrientation = screen.Orientation switch
            {
                AvnScreenOrientation.UnknownOrientation => ScreenOrientation.None,
                AvnScreenOrientation.Landscape => ScreenOrientation.Landscape,
                AvnScreenOrientation.Portrait => ScreenOrientation.Portrait,
                AvnScreenOrientation.LandscapeFlipped => ScreenOrientation.LandscapeFlipped,
                AvnScreenOrientation.PortraitFlipped => ScreenOrientation.PortraitFlipped,
                _ => throw new ArgumentOutOfRangeException()
            };

            using var avnString = MicroComRuntime.CreateProxyOrNullFor<IAvnString>(localizedName, true);
            DisplayName = avnString?.String;
        }
    }

    internal class ScreenImpl : ScreensBase<uint, AvnScreen>, IDisposable
    {
        private IAvnScreens _native;

        public ScreenImpl(Func<IAvnScreenEvents, IAvnScreens> factory)
        {
            using var events = new AvnScreenEvents(this);
            _native = factory(events);
        }

        protected override unsafe int GetScreenCount() => _native.GetScreenIds(null);

        protected override unsafe IReadOnlyList<uint> GetAllScreenKeys()
        {
            var screenCount = _native.GetScreenIds(null);
            var displayIds = new uint[screenCount];
            fixed (uint* displayIdsPtr = displayIds)
            {
                _native.GetScreenIds(displayIdsPtr);
            }

            return displayIds;
        }

        protected override AvnScreen CreateScreenFromKey(uint key) => new(key);
        protected override void ScreenChanged(AvnScreen screen) => screen.Refresh(_native);

        protected override Screen? ScreenFromTopLevelCore(ITopLevelImpl topLevel)
        {
            var displayId = ((TopLevelImpl)topLevel).Native?.CurrentDisplayId;
            return displayId is not null && TryGetScreen(displayId.Value, out var screen) ? screen : null;
        }

        public void Dispose()
        {
            _native?.Dispose();
            _native = null!;
        }

        private class AvnScreenEvents(ScreenImpl screenImpl) : NativeCallbackBase, IAvnScreenEvents
        {
            public void OnChanged() => screenImpl.OnChanged();
        }
    }
}
