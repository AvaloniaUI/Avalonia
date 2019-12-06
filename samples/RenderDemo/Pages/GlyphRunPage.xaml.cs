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
        private DrawingPresenter _drawingPresenter;
        private GlyphTypeface _glyphTypeface = Typeface.Default.GlyphTypeface;
        private readonly Random _rand = new Random();
        private ushort[] _glyphIndices = new ushort[1];
        private float _fontSize = 20;
        private int _direction = 10;

        public GlyphRunPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _drawingPresenter = this.FindControl<DrawingPresenter>("drawingPresenter");

            DispatcherTimer.Run(() =>
            {
                UpdateGlyphRun();

                return true;
            }, TimeSpan.FromSeconds(1));
        }

        private void UpdateGlyphRun()
        {
            var c = (uint)_rand.Next(65, 90);

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

            var scale = (double)_fontSize / _glyphTypeface.DesignEmHeight;

            var drawingGroup = new DrawingGroup();

            var glyphRunDrawing = new GlyphRunDrawing
            {
                Foreground = Brushes.Black,
                GlyphRun = new GlyphRun(_glyphTypeface, _fontSize, _glyphIndices),
                BaselineOrigin = new Point(0, -_glyphTypeface.Ascent * scale)
            };

            drawingGroup.Children.Add(glyphRunDrawing);

            var geometryDrawing = new GeometryDrawing
            {
                Pen = new Pen(Brushes.Black),
                Geometry = new RectangleGeometry { Rect = glyphRunDrawing.GlyphRun.Bounds }
            };

            drawingGroup.Children.Add(geometryDrawing);

            _drawingPresenter.Drawing = drawingGroup;
        }
    }
}
