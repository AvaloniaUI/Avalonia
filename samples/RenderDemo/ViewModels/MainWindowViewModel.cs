using System.Threading.Tasks;
using MiniMvvm;

namespace RenderDemo.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private bool _drawDirtyRects;
        private bool _drawFps = true;
        private bool _drawLayoutTimeGraph;
        private bool _drawRenderTimeGraph;
        private double _width = 800;
        private double _height = 600;

        public MainWindowViewModel()
        {
            ToggleDrawDirtyRects = MiniCommand.Create(() => DrawDirtyRects = !DrawDirtyRects);
            ToggleDrawFps = MiniCommand.Create(() => DrawFps = !DrawFps);
            ToggleDrawLayoutTimeGraph = MiniCommand.Create(() => DrawLayoutTimeGraph = !DrawLayoutTimeGraph);
            ToggleDrawRenderTimeGraph = MiniCommand.Create(() => DrawRenderTimeGraph = !DrawRenderTimeGraph);
            ResizeWindow = MiniCommand.CreateFromTask(ResizeWindowAsync);
        }

        public bool DrawDirtyRects
        {
            get => _drawDirtyRects;
            set => RaiseAndSetIfChanged(ref _drawDirtyRects, value);
        }

        public bool DrawFps
        {
            get => _drawFps;
            set => RaiseAndSetIfChanged(ref _drawFps, value);
        }

        public bool DrawLayoutTimeGraph
        {
            get => _drawLayoutTimeGraph;
            set => RaiseAndSetIfChanged(ref _drawLayoutTimeGraph, value);
        }

        public bool DrawRenderTimeGraph
        {
            get => _drawRenderTimeGraph;
            set => RaiseAndSetIfChanged(ref _drawRenderTimeGraph, value);
        }

        public double Width
        {
            get => _width;
            set => RaiseAndSetIfChanged(ref _width, value);
        }

        public double Height
        {
            get => _height;
            set => RaiseAndSetIfChanged(ref _height, value);
        }

        public MiniCommand ToggleDrawDirtyRects { get; }
        public MiniCommand ToggleDrawFps { get; }
        public MiniCommand ToggleDrawLayoutTimeGraph { get; }
        public MiniCommand ToggleDrawRenderTimeGraph { get; }
        public MiniCommand ResizeWindow { get; }

        private async Task ResizeWindowAsync()
        {
            for (int i = 0; i < 30; i++)
            {
                Width += 10;
                Height += 5;
                await Task.Delay(10);
            }

            await Task.Delay(10);

            for (int i = 0; i < 30; i++)
            {
                Width -= 10;
                Height -= 5;
                await Task.Delay(10);
            }
        }
    }
}
