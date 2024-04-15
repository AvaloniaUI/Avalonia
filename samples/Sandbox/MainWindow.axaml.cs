using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input.TextInput;
using Avalonia.Markup.Xaml;
using Avalonia.Win32.WinRT.Composition;

namespace Sandbox
{
    using System;
    using Avalonia.Threading;
    using DataGridAsyncDemoMVVM;
    using VitalElement.DataVirtualization;

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            if (!VirtualizationManager.IsInitialized)
            {
                //set the VirtualizationManager’s UIThreadExcecuteAction. In this case
                //we’re using Dispatcher.Invoke to give the VirtualizationManager access
                //to the dispatcher thread, and using a DispatcherTimer to run the background
                //operations the VirtualizationManager needs to run to reclaim pages and manage memory.
                VirtualizationManager.Instance.UiThreadExcecuteAction = a =>
                {
                    return Dispatcher.UIThread.InvokeAsync(a).GetTask();
                };

                DispatcherTimer.Run(() =>
                {
                    VirtualizationManager.Instance.ProcessActions();
                    return true;
                }, TimeSpan.FromMilliseconds(10), DispatcherPriority.Background	);
            }
            
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
