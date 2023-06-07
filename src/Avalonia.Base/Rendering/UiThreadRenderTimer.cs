using System;
using System.Diagnostics;
using Avalonia.Metadata;
using Avalonia.Reactive;
using Avalonia.Threading;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Render timer that ticks on UI thread. Useful for debugging or bootstrapping on new platforms 
    /// </summary>
    [PrivateApi]
    public class UiThreadRenderTimer : DefaultRenderTimer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UiThreadRenderTimer"/> class.
        /// </summary>
        /// <param name="framesPerSecond">The number of frames per second at which the loop should run.</param>
        public UiThreadRenderTimer(int framesPerSecond) : base(framesPerSecond)
        {
        }

        /// <inheritdoc />
        public override bool RunsInBackground => false;

        /// <inheritdoc />
        protected override IDisposable StartCore(Action<TimeSpan> tick)
        {
            bool cancelled = false;
            var st = Stopwatch.StartNew();
            DispatcherTimer.Run(() =>
            {
                if (cancelled)
                    return false;
                tick(st.Elapsed);
                return !cancelled;
            }, TimeSpan.FromSeconds(1.0 / FramesPerSecond), DispatcherPriority.UiThreadRender);
            return Disposable.Create(() => cancelled = true);
        }
    }
}
