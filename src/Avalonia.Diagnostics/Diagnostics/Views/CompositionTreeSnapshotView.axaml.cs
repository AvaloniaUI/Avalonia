using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;

namespace Avalonia.Diagnostics.Views;

internal class CompositionTreeSnapshotView : TypedUserControl<CompositionTreeSnapshotViewModel>
{
    private readonly Image _elementPicker;
    public CompositionTreeSnapshotView()
    {
        AvaloniaXamlLoader.Load(this);
        _elementPicker = this.FindControl<Image>("ElementPicker")!;
    }
    
    protected override void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CompositionTreeSnapshotViewModel.SelectedNode))
            DispatcherTimer.RunOnce(() =>
            {
                var item = this.GetLogicalDescendants().OfType<TreeViewItem>()
                    .FirstOrDefault(i => i.DataContext == Model!.SelectedNode);
                item?.BringIntoView();
            }, TimeSpan.FromMilliseconds(20));
    }

    protected override void Unsubscribe()
    {
        Model!.IsPicking = false;
        base.Unsubscribe();
    }

    private void PickElement(object sender, PointerPressedEventArgs e)
    {
        var pos = e.GetPosition(_elementPicker);
        var scale = Stretch.Uniform.CalculateScaling(Bounds.Size, _elementPicker.Bounds.Size);
        pos = new Point(pos.X * scale.X, pos.Y * scale.Y);
        Model?.PickItem(pos);
    }

    private void FreezeMe(object? sender, RoutedEventArgs e)
    {
        Model?.RootItems?.Clear();
    }
}