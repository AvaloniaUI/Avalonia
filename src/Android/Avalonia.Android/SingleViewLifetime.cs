using System;
using Android.App;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Android
{
    internal class SingleViewLifetime : ISingleViewApplicationLifetime, IActivatableApplicationLifetime
    {
        private readonly Activity _activity;
        private AvaloniaView _view;

        public SingleViewLifetime(Activity activity)
        {
            _activity = activity;

            if (activity is IAvaloniaActivity activableActivity)
            { 
                activableActivity.Activated += (_, args) => Activated?.Invoke(this, args);
                activableActivity.Deactivated += (_, args) => Deactivated?.Invoke(this, args);
            }
        }
        
        public AvaloniaView View
        {
            get => _view; internal set
            {
                if (_view != null)
                {
                    _view.Content = null;
                    _view.Dispose();
                }
                _view = value;
                _view.Content = MainView;
            }
        }

        public Control MainView { get; set; }
        public event EventHandler<ActivatedEventArgs> Activated;
        public event EventHandler<ActivatedEventArgs> Deactivated;

        public bool TryLeaveBackground() => _activity.MoveTaskToBack(true);
        public bool TryEnterBackground() => false;
    }
}
