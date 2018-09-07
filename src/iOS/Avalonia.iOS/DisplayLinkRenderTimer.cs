using System;
using Avalonia.Rendering;
using CoreAnimation;
using Foundation;

namespace Avalonia.iOS
{
    class DisplayLinkRenderTimer : IRenderTimer
    {
        public event Action<long> Tick;
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
                Tick?.Invoke(Environment.TickCount);
            }
            catch (Exception)
            {
                //TODO: log
            }
        }
    }
}
