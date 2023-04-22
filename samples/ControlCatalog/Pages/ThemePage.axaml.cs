using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace ControlCatalog.Pages
{
    public partial class ThemePage : UserControl
    {
        public static ThemeVariant Pink { get; } = new("Pink", ThemeVariant.Light);
        
        public ThemePage()
        {
            InitializeComponent();

            Selector.ItemsSource = new[]
            {
                ThemeVariant.Default,
                ThemeVariant.Dark,
                ThemeVariant.Light,
                Pink
            };
            Selector.SelectedIndex = 0;

            Selector.SelectionChanged += (_, _) =>
            {
                if (Selector.SelectedItem is ThemeVariant theme)
                {
                    ThemeVariantScope.RequestedThemeVariant = theme;
                }
            };
        }
    }
}
