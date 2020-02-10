using System;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.MessageBox;
using Avalonia.Platform;
using ReactiveUI;

namespace ControlCatalog.Pages
{
    public class MessageBoxPage : UserControl
    {
        public string Caption { get; set; } = "Hello";
        public string Message { get; set; } = "Welcome to Avalonia!";
        public bool ShowIcon { get; set; } = true;
        public ReactiveCommand<Unit, Unit> ShowOkCommand { get; }
        public ReactiveCommand<Unit, Unit> ShowOkCancelCommand { get; }
        public ReactiveCommand<Unit, Unit> ShowYesNoCommand { get; }
        public ReactiveCommand<Unit, Unit> ShowYesNoCancelCommand { get; }
        private Bitmap ExampleIcon;
        public MessageBoxPage()
        {
            this.InitializeComponent();
            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            ExampleIcon = new Bitmap(assets.Open(new Uri("avares://ControlCatalog/Assets/test_icon.ico")));

            ShowOkCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var result = await MessageBoxManager.Show(Message, Caption, MessageBoxButton.OK, ShowIcon ? ExampleIcon : null);
                ShowResult(result);
            });
            ShowOkCancelCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var result = await MessageBoxManager.Show(Message, Caption, MessageBoxButton.OK | MessageBoxButton.Cancel, ShowIcon ? ExampleIcon : null);
                ShowResult(result);
            });
            ShowYesNoCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var result = await MessageBoxManager.Show(Message, Caption, MessageBoxButton.Yes | MessageBoxButton.No, ShowIcon ? ExampleIcon : null);
                ShowResult(result);
            });
            ShowYesNoCancelCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var result = await MessageBoxManager.Show(Message, Caption, MessageBoxButton.Yes | MessageBoxButton.No | MessageBoxButton.Cancel, ShowIcon ? ExampleIcon : null);
                ShowResult(result);
            });
            DataContext = this;
        }

        private async void ShowResult(MessageBoxButton button)
        {
            await MessageBoxManager.Show($"You said '{button}'", "Result", MessageBoxButton.OK);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
