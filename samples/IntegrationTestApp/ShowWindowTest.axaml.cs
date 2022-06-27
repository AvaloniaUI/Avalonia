using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Rendering;

namespace IntegrationTestApp
{
    public class ShowWindowTest : Window
    {
        public ShowWindowTest()
        {
            InitializeComponent();
            DataContext = this;
            PositionChanged += (s, e) => this.GetControl<TextBox>("Position").Text = $"{Position}";
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            var scaling = PlatformImpl!.DesktopScaling;
            this.GetControl<TextBox>("Position").Text = $"{Position}";
            this.GetControl<TextBox>("ScreenRect").Text = $"{Screens.ScreenFromVisual(this)?.WorkingArea}";
            this.GetControl<TextBox>("Scaling").Text = $"{scaling}";

            if (Owner is not null)
            {
                var ownerRect = this.GetControl<TextBox>("OwnerRect");
                var owner = (Window)Owner;
                ownerRect.Text = $"{owner.Position}, {PixelSize.FromSize(owner.FrameSize!.Value, scaling)}";
            }
        }
    }
}
