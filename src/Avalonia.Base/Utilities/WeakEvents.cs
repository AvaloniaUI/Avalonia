using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;

namespace Avalonia.Utilities;

public class WeakEvents
{
    /// <summary>
    /// Represents CollectionChanged event from <see cref="INotifyCollectionChanged"/>
    /// </summary>
    public static readonly WeakEvent<INotifyCollectionChanged, NotifyCollectionChangedEventArgs>
        CollectionChanged = WeakEvent.Register<INotifyCollectionChanged, NotifyCollectionChangedEventArgs>(
            (c, s) =>
            {
                NotifyCollectionChangedEventHandler handler = (_, e) => s(c, e);
                c.CollectionChanged += handler;
                return () => c.CollectionChanged -= handler;
            });
    
    /// <summary>
    /// Represents PropertyChanged event from <see cref="INotifyPropertyChanged"/>
    /// </summary>
    public static readonly WeakEvent<INotifyPropertyChanged, PropertyChangedEventArgs>
        PropertyChanged = WeakEvent.Register<INotifyPropertyChanged, PropertyChangedEventArgs>(
            (s, h) =>
            {
                PropertyChangedEventHandler handler = (_, e) => h(s, e);
                s.PropertyChanged += handler;
                return () => s.PropertyChanged -= handler;
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
