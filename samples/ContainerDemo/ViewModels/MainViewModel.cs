using System;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using MiniMvvm;

namespace ContainerDemo.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private bool _isPaneOpen = true;

        public bool IsPaneOpen
        {
            get => _isPaneOpen;
            set
            {
                _isPaneOpen = value;

                this.RaisePropertyChanged();
            }
        }

        public void TogglePaneOpen()
        {
            IsPaneOpen = !IsPaneOpen;
        }
    }
}
