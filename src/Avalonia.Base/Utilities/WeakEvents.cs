using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using Avalonia.Threading;

namespace Avalonia.Utilities;

public class WeakEvents
{
    /// <summary>
    /// Represents CollectionChanged event from <see cref="INotifyCollectionChanged"/> with auto-dispatching to the UI thread
    /// </summary>
    public static readonly WeakEvent<INotifyCollectionChanged, NotifyCollectionChangedEventArgs>
        ThreadSafeCollectionChanged = WeakEvent.Register<INotifyCollectionChanged, NotifyCollectionChangedEventArgs>(
            (c, s) =>
            {
                bool unsubscribed = false;
                NotifyCollectionChangedEventHandler handler = (_, e) =>
                {
                    if (Dispatcher.UIThread.CheckAccess())
                        s(c, e);
                    else
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (!unsubscribed)
                                s(c, e);
                        });
                };
                c.CollectionChanged += handler;
                return () =>
                {
                    unsubscribed = true;
                    c.CollectionChanged -= handler;
                };
            });
    
    /// <summary>
    /// Represents PropertyChanged event from <see cref="INotifyPropertyChanged"/> with auto-dispatching to the UI thread
    /// </summary>
    public static readonly WeakEvent<INotifyPropertyChanged, PropertyChangedEventArgs>
        ThreadSafePropertyChanged = WeakEvent.Register<INotifyPropertyChanged, PropertyChangedEventArgs>(
            (s, h) =>
            {
                bool unsubscribed = false;
                PropertyChangedEventHandler handler = (_, e) =>
                {
                    if (Dispatcher.UIThread.CheckAccess())
                        h(s, e);
                    else
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (!unsubscribed)
                                h(s, e);
                        });
                };
                s.PropertyChanged += handler;
                return () =>
                {
                    unsubscribed = true;
                    s.PropertyChanged -= handler;
                };
            });


    /// <summary>
    /// Represents PropertyChanged event from <see cref="AvaloniaObject"/>
    /// </summary>
    public static readonly WeakEvent<AvaloniaObject, AvaloniaPropertyChangedEventArgs>
        AvaloniaPropertyChanged = WeakEvent.Register<AvaloniaObject, AvaloniaPropertyChangedEventArgs>(
            (s, h) =>
            {
                EventHandler<AvaloniaPropertyChangedEventArgs> handler = (_, e) => h(s, e);
                s.PropertyChanged += handler;
                return () => s.PropertyChanged -= handler;
            });

    /// <summary>
    /// Represents CanExecuteChanged event from <see cref="ICommand"/>
    /// </summary>
    public static readonly WeakEvent<ICommand, EventArgs> CommandCanExecuteChanged =
        WeakEvent.Register<ICommand>((s, h) => s.CanExecuteChanged += h,
            (s, h) => s.CanExecuteChanged -= h);
}
