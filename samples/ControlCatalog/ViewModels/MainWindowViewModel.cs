using System.Reactive;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Dialogs;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using MathNet;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    class MainWindowViewModel : ViewModelBase
    {
        private IManagedNotificationManager _notificationManager;

        private bool _isMenuItemChecked = true;
        private WindowState _windowState;
        private WindowState[] _windowStates;
        private int _transparencyLevel;
        private ExtendClientAreaChromeHints _chromeHints;
        private bool _extendClientAreaEnabled;
        private bool _systemTitleBarEnabled;        
        private bool _preferSystemChromeEnabled;
        private double _titleBarHeight;
        
        public  double[] XAxisValues { get; }
        
        public double[] YAxisValues { get; }
        
        public string[] XAxisLabels { get; }

        public MainWindowViewModel(IManagedNotificationManager notificationManager)
        {
            _notificationManager = notificationManager;

            ShowCustomManagedNotificationCommand = MiniCommand.Create(() =>
            {
                NotificationManager.Show(new NotificationViewModel(NotificationManager) { Title = "Hey There!", Message = "Did you know that Avalonia now supports Custom In-Window Notifications?" });
            });

            ShowManagedNotificationCommand = MiniCommand.Create(() =>
            {
                NotificationManager.Show(new Avalonia.Controls.Notifications.Notification("Welcome", "Avalonia now supports Notifications.", NotificationType.Information));
            });

            ShowNativeNotificationCommand = MiniCommand.Create(() =>
            {
                NotificationManager.Show(new Avalonia.Controls.Notifications.Notification("Error", "Native Notifications are not quite ready. Coming soon.", NotificationType.Error));
            });

            AboutCommand = MiniCommand.CreateFromTask(async () =>
            {
                var dialog = new AboutAvaloniaDialog();

                var mainWindow = (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

                await dialog.ShowDialog(mainWindow);
            });

            ExitCommand = MiniCommand.Create(() =>
            {
                (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).Shutdown();
            });

            ToggleMenuItemCheckedCommand = MiniCommand.Create(() =>
            {
                IsMenuItemChecked = !IsMenuItemChecked;
            });

            WindowState = WindowState.Normal;

            WindowStates = new WindowState[]
            {
                WindowState.Minimized,
                WindowState.Normal,
                WindowState.Maximized,
                WindowState.FullScreen,
            };

            this.WhenAnyValue(x => x.SystemTitleBarEnabled, x=>x.PreferSystemChromeEnabled)
                .Subscribe(x =>
                {
                    var hints = ExtendClientAreaChromeHints.NoChrome | ExtendClientAreaChromeHints.OSXThickTitleBar;

                    if(x.Item1)
                    {
                        hints |= ExtendClientAreaChromeHints.SystemChrome;
                    }

                    if(x.Item2)
                    {
                        hints |= ExtendClientAreaChromeHints.PreferSystemChrome;
                    }

                    ChromeHints = hints;
                });

            SystemTitleBarEnabled = true;            
            TitleBarHeight = -1;
            
            string[] xAxisLabels;
            double[] xAxisValues;
            double[] yAxisValues;
            
            {

                GetSmoothValuesSubdivide(s_xAxisValues, s_yAxisValues, out var ts, out var xts);
                xAxisValues = ts.ToArray();
                yAxisValues = xts.ToArray();
                
                // xAxisLabels = TestNetXAxisLabels;
                var labels = s_xAxisValues.Select(x => x.ToString())
                    .Reverse()
                    .ToArray();
                xAxisLabels = labels;
            }

            XAxisLabels = xAxisLabels;
            XAxisValues = xAxisValues;
            YAxisValues = yAxisValues;
        }
        
        private void GetSmoothValuesSubdivide(double[] xs, double[] ys, out List<double> ts, out List<double> xts)
        {
            const int Divisions = 256;

            ts = new List<double>();
            xts = new List<double>();

            if (xs.Length > 2)
            {
                var spline = CubicSpline.InterpolatePchipSorted(xs, ys);

                for (var i = 0; i < xs.Length - 1; i++)
                {
                    var a = xs[i];
                    var b = xs[i + 1];
                    var range = b - a;
                    var step = range / Divisions;

                    var t0 = xs[i];
                    ts.Add(t0);
                    var xt0 = spline.Interpolate(xs[i]);
                    xts.Add(xt0);

                    for (var t = a + step; t < b; t += step)
                    {
                        var xt = spline.Interpolate(t);
                        ts.Add(t);
                        xts.Add(xt);
                    }
                }

                var tn = xs[xs.Length - 1];
                ts.Add(tn);
                var xtn = spline.Interpolate(xs[xs.Length - 1]);
                xts.Add(xtn);
            }
            else
            {
                for (var i = 0; i < xs.Length; i++)
                {
                    ts.Add(xs[i]);
                    xts.Add(ys[i]);
                }
            }

            ts.Reverse();
            xts.Reverse();
        }

        public int TransparencyLevel
        {
            get { return _transparencyLevel; }
            set { this.RaiseAndSetIfChanged(ref _transparencyLevel, value); }
        }        

        public ExtendClientAreaChromeHints ChromeHints
        {
            get { return _chromeHints; }
            set { this.RaiseAndSetIfChanged(ref _chromeHints, value); }
        }        

        public bool ExtendClientAreaEnabled
        {
            get { return _extendClientAreaEnabled; }
            set { this.RaiseAndSetIfChanged(ref _extendClientAreaEnabled, value); }
        }        

        public bool SystemTitleBarEnabled
        {
            get { return _systemTitleBarEnabled; }
            set { this.RaiseAndSetIfChanged(ref _systemTitleBarEnabled, value); }
        }        

        public bool PreferSystemChromeEnabled
        {
            get { return _preferSystemChromeEnabled; }
            set { this.RaiseAndSetIfChanged(ref _preferSystemChromeEnabled, value); }
        }        

        public double TitleBarHeight
        {
            get { return _titleBarHeight; }
            set { this.RaiseAndSetIfChanged(ref _titleBarHeight, value); }
        }

        public WindowState WindowState
        {
            get { return _windowState; }
            set { this.RaiseAndSetIfChanged(ref _windowState, value); }
        }

        public WindowState[] WindowStates
        {
            get { return _windowStates; }
            set { this.RaiseAndSetIfChanged(ref _windowStates, value); }
        }

        public IManagedNotificationManager NotificationManager
        {
            get { return _notificationManager; }
            set { this.RaiseAndSetIfChanged(ref _notificationManager, value); }
        }

        public bool IsMenuItemChecked
        {
            get { return _isMenuItemChecked; }
            set { this.RaiseAndSetIfChanged(ref _isMenuItemChecked, value); }
        }

        public MiniCommand ShowCustomManagedNotificationCommand { get; }

        public MiniCommand ShowManagedNotificationCommand { get; }

        public MiniCommand ShowNativeNotificationCommand { get; }

        public MiniCommand AboutCommand { get; }

        public MiniCommand ExitCommand { get; }

        public MiniCommand ToggleMenuItemCheckedCommand { get; }
        
        private static readonly string[] s_xAxisLabels =
        {
            "1w",
            "3d",
            "1d",
            "12h",
            "6h",
            "3h",
            "1h",
            "30m",
            "20m",
            "fastest"
        };

        private static readonly double[] s_xAxisValues =
        {
            1,
            2,
            3,
            6,
            18,
            36,
            72,
            144,
            432,
            1008
        };

        private static readonly double[] s_yAxisValues =
        {
            185,
            123,
            123,
            102,
            97,
            57,
            22,
            7,
            4,
            4
        };
    }
}
