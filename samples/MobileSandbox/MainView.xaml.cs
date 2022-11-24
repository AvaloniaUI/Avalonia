using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MobileSandbox.Views;

namespace MobileSandbox
{
    public class MainView : UserControl
    {
        private TransitioningContentControl? _contentControl;
        private Label? _currentLabel;
        private Content _content;

        public MainView()
        {
            AvaloniaXamlLoader.Load(this);

            _contentControl = this.Find<TransitioningContentControl>("Con");
            _currentLabel = this.Find<Label>("Current");

            if(_currentLabel != null)
            {
                _currentLabel.Content = "Showing Nothing";
            }

            if(_contentControl != null)
            {
                _contentControl.Content = _content;
            }

            _content = new Content();

            DataContext = this;
        }

        public void ButtonCommand()
        {
            if(_contentControl!= null && _currentLabel != null)
            {
                _contentControl.Content = _contentControl.Content == _content ? new Label() { Content = "Hello There" } : _content;
                _currentLabel.Content = _contentControl.Content == _content ? "Should be showing button and label" : "Showing Hello";
            }
        }
    }
}
