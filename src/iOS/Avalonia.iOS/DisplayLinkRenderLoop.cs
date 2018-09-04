using System;
using Avalonia.Rendering;
using CoreAnimation;
using Foundation;

namespace Avalonia.iOS
{
    class DisplayLinkRenderLoop : IRenderLoop
    {
        public event EventHandler<EventArgs> Tick;
        private CADisplayLink _link;
        public DisplayLinkRenderLoop()
        {

            _link = CADisplayLink.Create(OnFrame);
            _link.AddToRunLoop(NSRunLoop.Main, NSRunLoop.NSDefaultRunLoopMode);
        }

        private void OnFrame()
        {
            try
            {
                Tick?.Invoke(this, new EventArgs());
            }
            catch (Exception)
            {
                //TODO: log
            }
        }
    }
}
