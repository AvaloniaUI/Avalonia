using System;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace VirtualizationDemo.Views;

public partial class PlaygroundPageView : UserControl
{
    private DispatcherTimer _timer;

    public PlaygroundPageView()
    {
        InitializeComponent();
        
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500),
        };
        
        _timer.Tick += TimerTick;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _timer.Stop();
    }

    private void TimerTick(object? sender, EventArgs e)
    {
        var message = $"Realized {list.GetRealizedContainers().Count()} of {list.ItemsPanelRoot?.Children.Count}";
        itemCount.Text = message;
    }
}
