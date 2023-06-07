using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Reactive;

namespace Avalonia.Platform
{
    internal class VisualMediaProvider : IMediaProvider
    {
        private readonly Visual _visual;

        public VisualMediaProvider(Visual visual)
        {
            _visual = visual;

            _visual.PropertyChanged += Visual_PropertyChanged;
        }

        private void Visual_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if(e.Property == Visual.BoundsProperty)
            {
                var oldValue = e.GetOldValue<Rect>();
                var newValue = e.GetNewValue<Rect>();

                if (oldValue.Size != newValue.Size)
                {
                    ScreenSizeChanged?.Invoke(_visual, EventArgs.Empty);
                }
            }
        }

        public event EventHandler? ScreenSizeChanged;

        public double GetScreenHeight() => _visual.Bounds.Size.Height;

        public double GetScreenWidth() => _visual.Bounds.Size.Width;
    }
}
