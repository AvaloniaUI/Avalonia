using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Platform;
using Avalonia.Rendering;
using MonoMac.Foundation;

namespace Avalonia.MonoMac
{
    //TODO: Switch to using CVDisplayLink
    public class RenderTimer : DefaultRenderTimer
    {
        public RenderTimer(int framesPerSecond) : base(framesPerSecond)
        {
        }

        protected override IDisposable StartCore(Action<long> tick)
        {
            return AvaloniaLocator.Current.GetService<IRuntimePlatform>().StartSystemTimer(
                TimeSpan.FromSeconds(1.0 / FramesPerSecond),
                () =>
                {
                    using (new NSAutoreleasePool())
                    {
                        tick(TimeStampToFrames());
                    }
                });
        }
    }
}
