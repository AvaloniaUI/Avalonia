using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;

namespace RenderDemo.Pages
{
    public class GlyphRunPage : UserControl
    {
        public GlyphRunPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }

    public class GlyphRunControl : Control
    {
        private IGlyphTypeface _glyphTypeface = Typeface.Default.GlyphTypeface;
        private readonly Random _rand = new Random();
        private ushort[] _glyphIndices = new ushort[1];
        private char[] _characters = new char[1];
        private float _fontSize = 20;
        private int _direction = 10;

        private DispatcherTimer _timer;

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _timer.Tick += (s,e) =>
            {
                InvalidateVisual();
            };

            _timer.Start();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _timer.Stop();

            _timer = null;
        }

        public override void Render(DrawingContext context)
        {
            var c = (char)_rand.Next(65, 90);

            if (_fontSize + _direction > 200)
            {
                _direction = -10;
            }

            if (_fontSize + _direction < 20)
            {
                _direction = 10;
            }

            _fontSize += _direction;

            _glyphIndices[0] = _glyphTypeface.GetGlyph(c);

            _characters[0] = c;

            var glyphRun = new GlyphRun(_glyphTypeface, _fontSize, _characters, _glyphIndices);

            context.DrawGlyphRun(Brushes.Black, glyphRun);
        }
    }

    public class GlyphRunGeometryControl : Control
    {
        private IGlyphTypeface _glyphTypeface = Typeface.Default.GlyphTypeface;
        private readonly Random _rand = new Random();
        private ushort[] _glyphIndices = new ushort[1];
        private char[] _characters = new char[1];
        private float _fontSize = 20;
        private int _direction = 10;

        private DispatcherTimer _timer;

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _timer.Tick += (s, e) =>
            {
                InvalidateVisual();
            };

            _timer.Start();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _timer.Stop();

            _timer = null;
        }

        public override void Render(DrawingContext context)
        {
            var c = (char)_rand.Next(65, 90);

            if (_fontSize + _direction > 200)
            {
                _direction = -10;
            }

            if (_fontSize + _direction < 20)
            {
                _direction = 10;
            }

            _fontSize += _direction;

            _glyphIndices[0] = _glyphTypeface.GetGlyph(c);

            _characters[0] = c;

            var glyphRun = new GlyphRun(_glyphTypeface, _fontSize, _characters, _glyphIndices);

            var geometry = glyphRun.BuildGeometry();          

            context.DrawGeometry(Brushes.Green, null, geometry);          
        }
    }
}
