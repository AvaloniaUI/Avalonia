using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class ContentPageSafeAreaPage : UserControl
    {
        public ContentPageSafeAreaPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            SyncIndicators();
        }

        private void OnAutoApplyChanged(object? sender, RoutedEventArgs e)
        {
            if (SamplePage == null)
                return;
            SamplePage.AutomaticallyApplySafeAreaPadding = AutoApplyCheck.IsChecked == true;
            SyncIndicators();
        }

        private void OnInsetChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            SyncIndicators();
        }

        private void SyncIndicators()
        {
            if (SamplePage == null)
                return;
            var top = (int)TopSlider.Value;
            var bottom = (int)BottomSlider.Value;
            var left = (int)LeftSlider.Value;
            var right = (int)RightSlider.Value;

            TopValue.Text = $"{top}";
            BottomValue.Text = $"{bottom}";
            LeftValue.Text = $"{left}";
            RightValue.Text = $"{right}";

            TopInsetIndicator.IsVisible = top > 0;
            TopInsetIndicator.Height = top;

            BottomInsetIndicator.IsVisible = bottom > 0;
            BottomInsetIndicator.Height = bottom;

            LeftInsetIndicator.IsVisible = left > 0;
            LeftInsetIndicator.Width = left;
            LeftInsetIndicator.Margin = new Thickness(0, top, 0, bottom);

            RightInsetIndicator.IsVisible = right > 0;
            RightInsetIndicator.Width = right;
            RightInsetIndicator.Margin = new Thickness(0, top, 0, bottom);

            var insets = new Thickness(left, top, right, bottom);
            SamplePage.SafeAreaPadding = insets;

            SafeAreaInfo.Text = $"SafeAreaPadding: L={left} T={top} R={right} B={bottom}";
            AutoApplyInfo.Text = $"AutoApply: {SamplePage.AutomaticallyApplySafeAreaPadding}  →  " +
                (SamplePage.AutomaticallyApplySafeAreaPadding ? "insets absorbed by presenter" : "insets ignored");
        }
    }
}
