using System;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Avalonia.Diagnostics.Views;

internal class TypedUserControl<T> : UserControl, IStyleable where T : class, INotifyPropertyChanged
{
    Type IStyleable.StyleKey => typeof(UserControl);
    
    protected T? Model { get; private set; }
    
    protected virtual void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        
    }

    protected virtual void Subscribe()
    {
        
    }
    
    protected virtual void Unsubscribe()
    {
        
    }
    
    void UpdateSubscriptions()
    {
        if (Model != null)
        {
            Model.PropertyChanged -= OnModelPropertyChanged;
            Unsubscribe();
        }

        Model = IsAttachedToVisualTree ? DataContext as T : null;
        if (Model != null)
        {
            Model.PropertyChanged += OnModelPropertyChanged;
            Subscribe();
        }
    }
    
    protected override void OnDataContextChanged(EventArgs e)
    {
        UpdateSubscriptions();
        base.OnDataContextChanged(e);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        UpdateSubscriptions();
        base.OnAttachedToVisualTree(e);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        UpdateSubscriptions();
        base.OnDetachedFromVisualTree(e);
    }
}