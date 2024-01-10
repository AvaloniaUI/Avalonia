using System;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Android
{
    public interface IActivityNavigationService
    {
        event EventHandler<AndroidBackRequestedEventArgs> BackRequested;
    }

    public class AndroidBackRequestedEventArgs : EventArgs
    {
        public bool Handled { get; set; }
    }
}
