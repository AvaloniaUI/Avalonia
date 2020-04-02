using System;
using Avalonia.Rendering;
using CoreAnimation;
using Foundation;

namespace Avalonia.iOS
{
    class DisplayLinkRenderTimer : IRenderTimer
    {
        public event Action<TimeSpan> Tick;
        private CADisplayLink _link;

        public DisplayLinkRenderTimer()
        {

            _link = CADisplayLink.Create(OnFrame);
            _link.AddToRunLoop(NSRunLoop.Main, NSRunLoop.NSDefaultRunLoopMode);
        }

        private void OnFrame()
        {
            try
            {
                Tick?.Invoke(TimeSpan.FromMilliseconds(Environment.TickCount));
            }
            catch (Exception)
            {
                //TODO: log
            }
        }
    }
}
