using System;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using SafeAreaDemo.ViewModels;

namespace SafeAreaDemo.Views
{
    public partial class MainView : UserControl
    {
        private TextBox? _testBox;

        public MainView()
        {
            AvaloniaXamlLoader.Load(this);

            _testBox = this.Find<TextBox>("TestTextBox");
        }

        /// <inheritdoc/>
        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            var insetsManager = TopLevel.GetTopLevel(this)?.InsetsManager;
            var inputPane = TopLevel.GetTopLevel(this)?.InputPane;
            var viewModel = new MainViewModel();
            viewModel.Initialize(this, insetsManager, inputPane);
            DataContext = viewModel;



            if (_testBox != null)
            {
                var presenter = _testBox.FindDescendantOfType<TextPresenter>();

                presenter?.EffectiveViewportChanged += MainView_EffectiveViewportChanged;
            }
        }

        private void MainView_EffectiveViewportChanged(object? sender, Avalonia.Layout.EffectiveViewportChangedEventArgs e)
        {
            Console.WriteLine("Focused TextBox Presenter viewport changed");
        }
    }
}
