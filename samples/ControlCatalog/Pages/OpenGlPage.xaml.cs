using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using ControlCatalog.Pages.OpenGl;

// ReSharper disable StringLiteralTypo

namespace ControlCatalog.Pages
{
    public class OpenGlPage : UserControl
    {
        public OpenGlPage()
        {
            AvaloniaXamlLoader.Load(this);
            this.FindControl<OpenGlPageControl>("GL")
                !.Init(this.FindControl<GlPageKnobs>("Knobs")!);
        }
    }

    public class OpenGlPageControl : OpenGlControlBase
    {
        private OpenGlContent _content = new();
        private GlPageKnobs? _knobs;

        public void Init(GlPageKnobs knobs)
        {
            _knobs = knobs;
            _knobs.PropertyChanged += KnobsPropertyChanged;
        }

        private void KnobsPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
        {
            if (change.Property == GlPageKnobs.YawProperty
                || change.Property == GlPageKnobs.RollProperty
                || change.Property == GlPageKnobs.PitchProperty
                || change.Property == GlPageKnobs.DiscoProperty)
                RequestNextFrameRendering();
        }
        
        protected override unsafe void OnOpenGlInit(GlInterface GL) => _content.Init(GL, GlVersion);

        protected override void OnOpenGlDeinit(GlInterface GL) => _content.Deinit(GL);

        protected override unsafe void OnOpenGlRender(GlInterface gl, int fb)
        {
            if (_knobs == null)
                return;
            _content.OnOpenGlRender(gl, fb, new PixelSize((int)Bounds.Width, (int)Bounds.Height),
                _knobs.Yaw, _knobs.Pitch, _knobs.Roll, _knobs.Disco);
            if (_knobs.Disco > 0.01)
                RequestNextFrameRendering();
        }
    }
}
