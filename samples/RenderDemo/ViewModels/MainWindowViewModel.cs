using System.Reactive;
using System.Threading.Tasks;

using ReactiveUI;

namespace RenderDemo.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private bool drawDirtyRects = false;
        private bool drawFps = true;
        private double width = 800;
        private double height = 600;

        public MainWindowViewModel()
        {
            ToggleDrawDirtyRects = ReactiveCommand.Create(() => DrawDirtyRects = !DrawDirtyRects);
            ToggleDrawFps = ReactiveCommand.Create(() => DrawFps = !DrawFps);
            ResizeWindow = ReactiveCommand.CreateFromTask(ResizeWindowAsync);
        }

        public bool DrawDirtyRects
        {
            get => drawDirtyRects;
            set => this.RaiseAndSetIfChanged(ref drawDirtyRects, value);
        }

        public bool DrawFps
        {
            get => drawFps;
            set => this.RaiseAndSetIfChanged(ref drawFps, value);
        }

        public double Width
        {
            get => width;
            set => this.RaiseAndSetIfChanged(ref width, value);
        }

        public double Height
        {
            get => height;
            set => this.RaiseAndSetIfChanged(ref height, value);
        }

        public ReactiveCommand<Unit, bool> ToggleDrawDirtyRects { get; }
        public ReactiveCommand<Unit, bool> ToggleDrawFps { get; }
        public ReactiveCommand<Unit, Unit> ResizeWindow { get; }

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
