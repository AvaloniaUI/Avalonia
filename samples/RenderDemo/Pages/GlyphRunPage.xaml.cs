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
        private Image _imageControl;
        private GlyphTypeface _glyphTypeface = Typeface.Default.GlyphTypeface;
        private readonly Random _rand = new Random();
        private ushort[] _glyphIndices = new ushort[1];
        private char[] _characters = new char[1];
        private float _fontSize = 20;
        private int _direction = 10;

        public GlyphRunPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _imageControl = this.FindControl<Image>("imageControl");
            _imageControl.Source = new DrawingImage();

            DispatcherTimer.Run(() =>
            {
                UpdateGlyphRun();

                return true;
            }, TimeSpan.FromSeconds(1));
        }

        private void UpdateGlyphRun()
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

            var scale = (double)_fontSize / _glyphTypeface.DesignEmHeight;

            var drawingGroup = new DrawingGroup();

            var glyphRunDrawing = new GlyphRunDrawing
            {
                Foreground = Brushes.Black,
                GlyphRun = new GlyphRun(_glyphTypeface, _fontSize, _characters, _glyphIndices)
            };

            drawingGroup.Children.Add(glyphRunDrawing);

            var geometryDrawing = new GeometryDrawing
            {
                Pen = new Pen(Brushes.Black),
                Geometry = new RectangleGeometry { Rect = new Rect(glyphRunDrawing.GlyphRun.Size) }
            };

            drawingGroup.Children.Add(geometryDrawing);

            (_imageControl.Source as DrawingImage).Drawing = drawingGroup;
        }
    }
}
