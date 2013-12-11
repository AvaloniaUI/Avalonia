namespace Perspex.Controls
{
    using Perspex.Media;

    public class TextBlock : Control
    {
        public static readonly PerspexProperty<Brush> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<TextBlock>();

        public static readonly PerspexProperty<Brush> ForegroundProperty =
            PerspexProperty.Register<TextBlock, Brush>(
                "Foreground",
                defaultValue: new SolidColorBrush(0xff000000),
                inherits: true);

        public static readonly PerspexProperty<string> TextProperty =
            PerspexProperty.Register<Border, string>("Text");

        public Brush Background
        {
            get { return this.GetValue(BackgroundProperty); }
            set { this.SetValue(BackgroundProperty, value); }
        }

        public Brush Foreground
        {
            get { return this.GetValue(ForegroundProperty); }
            set { this.SetValue(ForegroundProperty, value); }
        }

        public string Text
        {
            get { return this.GetValue(TextProperty); }
            set { this.SetValue(TextProperty, value); }
        }

        private FormattedText FormattedText
        {
            get
            {
                return new FormattedText
                {
                    FontFamilyName = "Segoe UI",
                    FontSize = 18,
                    Text = this.Text,
                };
            }
        }

        public override void Render(IDrawingContext context)
        {
            Brush background = this.Background;

            if (background != null)
            {
                context.FillRectange(background, this.Bounds);
            }

            context.DrawText(this.Foreground, this.Bounds, this.FormattedText);
        }

        protected override Size MeasureContent(Size availableSize)
        {
            ITextService service = ServiceLocator.Get<ITextService>();
            return service.Measure(this.FormattedText);
        }
    }
}
