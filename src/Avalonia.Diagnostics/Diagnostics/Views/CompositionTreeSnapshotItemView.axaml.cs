using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Avalonia.Diagnostics.Views;

internal partial class CompositionTreeSnapshotItemView : TypedUserControl<CompositionTreeSnapshotItemViewModel>
{
    private readonly Image _image;

    public CompositionTreeSnapshotItemView()
    {
        InitializeComponent();
        _image = this.FindControl<Image>("Image")!;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void Subscribe()
    {
        UpdateImage();
        base.Subscribe();
    }

    protected override void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Model.SelectedDrawOperationIndex))
            UpdateImage();
        base.OnModelPropertyChanged(sender, e);
    }

    private void ResetDrawOperationIndex(object? sender, RoutedEventArgs e)
    {
        if (Model != null)
            Model.SelectedDrawOperationIndex = -1;
    }

    async void UpdateImage()
    {
        var drawForModel = Model;
        var drawForIndex = Model!.SelectedDrawOperationIndex;
        await Task.Delay(100);
        if (drawForModel != Model || drawForIndex != drawForModel.SelectedDrawOperationIndex)
            return;
        var image = await Model!.Item.RenderToBitmapAsync(Model.SelectedDrawOperationIndex == -1 ? null : Model.SelectedDrawOperationIndex);
        if (drawForModel != Model || drawForIndex != drawForModel.SelectedDrawOperationIndex)
        {
            image?.Dispose();
            return;
        }

        var oldSource = _image.Source;
        _image.Source = image;
        (oldSource as IDisposable)?.Dispose();
    }
    
}