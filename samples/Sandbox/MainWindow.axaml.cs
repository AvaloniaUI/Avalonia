using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace Sandbox
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }

    public class Layer : Control
    {
        public static StyledProperty<string> TextProperty = AvaloniaProperty.Register<Layer, string>(nameof(Text));

        private TextLayout _textLayout;

        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public IReadOnlyList<TextLine> TextLines => _textLayout?.TextLines;

        public override void Render(DrawingContext context)
        {
            context.FillRectangle(Brushes.Magenta, new Rect(new Point(0, 0), Bounds.Size));
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            switch (change.Property.Name)
            {
                case nameof(Text):
                    {
                        InvalidateMeasure();
                        break;
                    }
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var fontFamily = TextElement.GetFontFamily(this);
            var fontSize = TextElement.GetFontSize(this);
            var foreground = TextElement.GetForeground(this);

            _textLayout = new TextLayout(Text, new Typeface(fontFamily), fontSize, foreground);

            var width = 0.0;
            var height = 0.0;

            VisualChildren.Clear();

            foreach (var line in TextLines)
            {
                if(line.WidthIncludingTrailingWhitespace > width)
                {
                    width = line.WidthIncludingTrailingWhitespace;
                }

                height += line.Height;

                var visualLine = new VisualLine(line, new Rect(0, height, line.WidthIncludingTrailingWhitespace, line.Height));

                VisualChildren.Add(visualLine);
            }

            return new Size(width, height);
        }

        public class VisualLine : Control
        {
            private Rect _bounds;

            public VisualLine(TextLine textLine, Rect bounds)
            {
                TextLine = textLine;
                _bounds = bounds;
            }

            public TextLine TextLine { get; }

            public override void Render(DrawingContext context)
            {
                TextLine.Draw(context, _bounds.Position);
            }
        }
    }
}
