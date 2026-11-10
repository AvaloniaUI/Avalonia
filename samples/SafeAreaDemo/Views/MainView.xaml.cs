using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using SafeAreaDemo.ViewModels;

namespace SafeAreaDemo.Views
{
    public partial class MainView : UserControl
    {
        public MainView()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <inheritdoc/>
        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            var caption = this.FindControl<Grid>("CaptionBar")!;
            var insetsManager = TopLevel.GetTopLevel(this)?.InsetsManager;
            var inputPane = TopLevel.GetTopLevel(this)?.InputPane;
            var viewModel = new MainViewModel();
            caption.SizeChanged += Caption_SizeChanged;
            viewModel.Initialize(this, insetsManager, inputPane);
            viewModel.SafeAreaChanged += ViewModel_SafeAreaChanged;
            DataContext = viewModel;
        }

        private void ViewModel_SafeAreaChanged(object? sender, System.EventArgs e)
        {
            SetClientBoundsInCaption();
        }

        private void SetClientBoundsInCaption()
        {
            if (TopLevel.GetTopLevel(this)?.InsetsManager is IInsetsWithCaptionManager captionManager
                && captionManager.ExtendClientIntoCaption && this.FindControl<Grid>("CaptionBar") is { } grid)
            {
                
                var transform = grid.TransformToVisual(this);
                var bounds = grid.Bounds.TransformToAABB(transform ?? Matrix.Identity);
                captionManager?.SetClientBoundsInCaption(bounds);
            }
        }

        private void Caption_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            SetClientBoundsInCaption();
        }
    }
}
