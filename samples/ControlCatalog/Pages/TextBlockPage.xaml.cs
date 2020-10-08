using System;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace ControlCatalog.Pages
{
    public class TextBlockPage : UserControl
    {
        public TextBlockPage()
        {
            this.InitializeComponent();

            var txtChanging = this.Get<TextBlock>("txtChanging");
            var tpChanging = this.Get<TextPresenter>("tpChanging");

            void update()
            {
                var time = DateTime.Now.TimeOfDay.ToString();
                txtChanging.Text = $"TextBlock: {time}";
                tpChanging.Text = $"TextPresenter: {time}";
            }
            var btn = this.Get<Button>("btn");
            btn.Click += (s, e) => update();
            //DispatcherTimer.Run(() =>
            //{
            //    update();
            //    return true;
            //}, TimeSpan.FromSeconds(1));

        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
