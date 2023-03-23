using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Markup.Xaml;
using Avalonia.Win32.WinRT.Composition;

namespace Sandbox
{
    public class MyViewModel : INotifyPropertyChanged
    {
        private string _foo;
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public string Foo
        {
            get => _foo;
            set
            {
                if (value == _foo) return;
                _foo = value;
                OnPropertyChanged();
            }
        }
    }
    
    public class MainWindow : Window
    {
        private readonly Timer _timer;

        public MainWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();
            var model = new MyViewModel();
            DataContext = model;
            int cnt = 0;
            _timer = new Timer(_ => model.Foo = (cnt++).ToString(), null, 0, 200);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
