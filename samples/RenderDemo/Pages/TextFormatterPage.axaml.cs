using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace RenderDemo.Pages
{
    public class TextFormatterPage : UserControl
    {
        private TextLine _textLine;
        
        public TextFormatterPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        public override void Render(DrawingContext context)
        {
            _textLine?.Draw(context, new Point());
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var defaultRunProperties = new GenericTextRunProperties(Typeface.Default, foregroundBrush: Brushes.Black,
                baselineAlignment: BaselineAlignment.Center);
            var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties);

            var control = new Button { Content = new TextBlock { Text = "ClickMe" } };
            
            Content = control;
            
            var textSource = new CustomTextSource(control, defaultRunProperties);

            control.Measure(Size.Infinity);

            _textLine =
                TextFormatter.Current.FormatLine(textSource, 0, double.PositiveInfinity, paragraphProperties);
            
            return base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var currentX = 0d;
            
            foreach (var textRun in _textLine.TextRuns)
            {
                if (textRun is ControlRun controlRun)
                {
                    controlRun.Control.Arrange(new Rect(new Point(currentX, 0), controlRun.Size));
                }

                if (textRun is DrawableTextRun drawableTextRun)
                {
                    currentX += drawableTextRun.Size.Width;
                }
            }
            
            return finalSize;
        }

        private class CustomTextSource : ITextSource
        {
            private readonly Control _control;
            private readonly TextRunProperties _defaultProperties;
            private readonly string _text = "<-Hello World->";

            public CustomTextSource(Control control, TextRunProperties defaultProperties)
            {
                _control = control;
                _defaultProperties = defaultProperties;
            }
            
            public TextRun GetTextRun(int textSourceIndex)
            {
                if (textSourceIndex >= _text.Length * 2 + TextRun.DefaultTextSourceLength)
                {
                    return null;
                }

                if (textSourceIndex == _text.Length)
                {
                    return new ControlRun(_control, _defaultProperties);
                }

                return new TextCharacters(_text, _defaultProperties);
            }
        }

        private class ControlRun : DrawableTextRun
        {
            private readonly Control _control;

            public ControlRun(Control control, TextRunProperties properties)
            {
                _control = control;
                Properties = properties;
            }

            public Control Control => _control;
            public override Size Size => _control.DesiredSize;
            public override double Baseline => 0;
            public override TextRunProperties Properties { get; }

            public override void Draw(DrawingContext drawingContext, Point origin)
            {
                // noop
            }
        }
    }
}
