using System.Reactive;
using System.Threading.Tasks;
using MiniMvvm;

namespace RenderDemo.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private bool drawDirtyRects = false;
        private bool drawFps = true;
        private double width = 800;
        private double height = 600;

        public MainWindowViewModel()
        {
            ToggleDrawDirtyRects = MiniCommand.Create(() => DrawDirtyRects = !DrawDirtyRects);
            ToggleDrawFps = MiniCommand.Create(() => DrawFps = !DrawFps);
            ResizeWindow = MiniCommand.CreateFromTask(ResizeWindowAsync);
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

        public MiniCommand ToggleDrawDirtyRects { get; }
        public MiniCommand ToggleDrawFps { get; }
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
