using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class CommandBarTogglePage : UserControl
    {
        public CommandBarTogglePage()
        {
            InitializeComponent();
        }

        private void OnFormatChanged(object? sender, RoutedEventArgs e)
        {
            if (FormatStatus == null)
                return;
            var active = new List<string>();
            if (BoldToggle.IsChecked == true) active.Add("Bold");
            if (ItalicToggle.IsChecked == true) active.Add("Italic");
            if (UnderlineToggle.IsChecked == true) active.Add("Underline");
            FormatStatus.Text = active.Count > 0 ? $"Active: {string.Join(", ", active)}" : "Active: (none)";
        }

        private void OnForceBoldChanged(object? sender, RoutedEventArgs e)
        {
            if (BoldToggle == null)
                return;
            BoldToggle.IsChecked = ForceBoldCheck.IsChecked;
        }

        private void OnForceItalicChanged(object? sender, RoutedEventArgs e)
        {
            if (ItalicToggle == null)
                return;
            ItalicToggle.IsChecked = ForceItalicCheck.IsChecked;
        }

        private void OnForceUnderlineChanged(object? sender, RoutedEventArgs e)
        {
            if (UnderlineToggle == null)
                return;
            UnderlineToggle.IsChecked = ForceUnderlineCheck.IsChecked;
        }
    }
}
