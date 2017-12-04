using System;
using ReactiveUI;

namespace RenderTest.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private bool drawDirtyRects = true;
        private bool drawFps = true;

        public MainWindowViewModel()
        {
            ToggleDrawDirtyRects = ReactiveCommand.Create(() => DrawDirtyRects = !DrawDirtyRects);
            ToggleDrawFps = ReactiveCommand.Create(() => DrawFps = !DrawFps);
        }

        public bool DrawDirtyRects
        {
            get { return drawDirtyRects; }
            set { this.RaiseAndSetIfChanged(ref drawDirtyRects, value); }
        }

        public bool DrawFps
        {
            get { return drawFps; }
            set { this.RaiseAndSetIfChanged(ref drawFps, value); }
        }

        public ReactiveCommand ToggleDrawDirtyRects { get; }
        public ReactiveCommand ToggleDrawFps { get; }
    }
}
