using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Platform;

namespace Avalonia.Android.Platform
{
    internal class AndroidMediaProvider : IMediaProvider
    {
        private readonly TopLevelImpl _topLevelImpl;

        public AndroidMediaProvider(TopLevelImpl topLevelImpl)
        {
            _topLevelImpl = topLevelImpl;
        }

        public void RaiseSizeChanged()
        {
            ScreenSizeChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler ScreenSizeChanged;

        public double GetScreenHeight() => _topLevelImpl.Size.Height;

        public double GetScreenWidth() => _topLevelImpl.Size.Width;
    }
}
