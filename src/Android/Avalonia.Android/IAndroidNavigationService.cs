using System;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Android
{
    public interface IActivityNavigationService
    {
        event EventHandler<AndroidBackRequestedEventArgs> BackRequested;
    }

    public interface IActivableActivity
    {
        event EventHandler<ActivatedEventArgs> Activated;
        event EventHandler<ActivatedEventArgs> Deactivated;
    }
    
    public class AndroidBackRequestedEventArgs : EventArgs
    {
        public bool Handled { get; set; }
    }
}
