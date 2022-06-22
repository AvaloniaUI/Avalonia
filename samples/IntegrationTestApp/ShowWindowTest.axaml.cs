using System;
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
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            this.GetControl<TextBox>("ClientSize").Text = $"{Width}, {Height}";
            this.GetControl<TextBox>("FrameSize").Text = $"{FrameSize}";
            this.GetControl<TextBox>("Position").Text = $"{Position}";
            this.GetControl<TextBox>("ScreenRect").Text = $"{Screens.ScreenFromVisual(this)?.WorkingArea}";
            this.GetControl<TextBox>("Scaling").Text = $"{PlatformImpl?.DesktopScaling}";

            if (Owner is not null)
            {
                var ownerRect = this.GetControl<TextBox>("OwnerRect");
                var owner = (Window)Owner;
                ownerRect.Text = $"{owner.Position}, {owner.FrameSize}";
            }
        }
    }
}
