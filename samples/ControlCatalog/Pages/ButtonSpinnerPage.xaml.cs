using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class ButtonSpinnerPage : UserControl
    {
        public ButtonSpinnerPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void OnSpin(object sender, SpinEventArgs e)
        {
            var spinner = (ButtonSpinner)sender;
            var txtBox = (TextBlock)spinner.Content;

            int value = Array.IndexOf(_mountains, txtBox.Text);
            if (e.Direction == SpinDirection.Increase)
                value++;
            else
                value--;

            if (value < 0)
                value = _mountains.Length - 1;
            else if (value >= _mountains.Length)
                value = 0;

            txtBox.Text = _mountains[value];
        }

        private readonly string[] _mountains = new[]
        {
            "Everest",
            "K2 (Mount Godwin Austen)",
            "Kangchenjunga",
            "Lhotse",
            "Makalu",
            "Cho Oyu",
            "Dhaulagiri",
            "Manaslu",
            "Nanga Parbat",
            "Annapurna"
        };
    }
}
