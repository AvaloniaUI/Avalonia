using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

namespace Avalonia.Diagnostics.ViewModels;

internal class CompositionTreeSnapshotViewModel : ViewModelBase, IDisposable
{
    private CompositionTreeSnapshotItemViewModel? _selectedNode;
    private bool _isPicking;
    private CompositionTreeSnapshot _snapshot;

    public CompositionTreeSnapshot Snapshot
    {
        get => _snapshot;
        private set => RaiseAndSetIfChanged(ref _snapshot, value);
    }

    public TopLevel TopLevel { get; }


    public AvaloniaList<CompositionTreeSnapshotItemViewModel> RootItems { get; }

    public bool IsPicking
    {
        get => _isPicking;
        set => RaiseAndSetIfChanged(ref _isPicking, value);
    }

    public void PickItem(Point pos)
    {
        IsPicking = false;
        var item = Snapshot.HitTest(pos);
        if (item != null) 
            SelectedNode = ExpandToItem(RootItems![0], item);
    }

    private CompositionTreeSnapshotItemViewModel? ExpandToItem(CompositionTreeSnapshotItemViewModel currentVm, CompositionTreeSnapshotItem item)
    {
        if (currentVm.Item == item)
        {
            SelectedNode = currentVm;
            return currentVm;
        }

        foreach (var ch in currentVm.Children)
        {
            var chRes = ExpandToItem(ch, item);
            if (chRes != null)
            {
                currentVm.IsExpanded = true;
                return chRes;
            }
        }

        return null;
    }

    public CompositionTreeSnapshotItemViewModel? SelectedNode
    {
        get => _selectedNode;
        set => RaiseAndSetIfChanged(ref _selectedNode, value);
    }
    
    public CompositionTreeSnapshotViewModel(TopLevel topLevel, CompositionTreeSnapshot snapshot)
    {
        _snapshot = snapshot;
        TopLevel = topLevel;
        RootItems = new(new CompositionTreeSnapshotItemViewModel(snapshot.Root));
    }
    
    public void Dispose()
    {
        IsPicking = false;
        Snapshot.DisposeAsync();
    }
}

internal class CompositionTreeSnapshotItemViewModel : ViewModelBase
{
    private bool _isExpanded = false;
    private CompositionTreeSnapshotItemPropertyViewModel? _selectedProperty;
    private int _selectedDrawOperationIndex;
    public CompositionTreeSnapshotItem Item { get; }
    public IReadOnlyList<CompositionTreeSnapshotItemViewModel> Children { get; }
    public IAvaloniaReadOnlyList<CompositionTreeSnapshotItemPropertyViewModel> Properties { get; }
    

    public CompositionTreeSnapshotItemPropertyViewModel? SelectedProperty
    {
        get => _selectedProperty;
        set => RaiseAndSetIfChanged(ref _selectedProperty, value);
    }

    public int SelectedDrawOperationIndex
    {
        get => _selectedDrawOperationIndex;
        set => RaiseAndSetIfChanged(ref _selectedDrawOperationIndex, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => RaiseAndSetIfChanged(ref _isExpanded, value);
    }

    public CompositionTreeSnapshotItemViewModel(CompositionTreeSnapshotItem item)
    {
        Item = item;
        Children = item.Children.Select(x => new CompositionTreeSnapshotItemViewModel(x)).ToList();
        Properties =
            new AvaloniaList<CompositionTreeSnapshotItemPropertyViewModel>(item.Properties
                .Select(x => new CompositionTreeSnapshotItemPropertyViewModel(x.Key, x.Value))
                .OrderBy(x => x.Name));
    }
}

internal class CompositionTreeSnapshotItemPropertyViewModel : ViewModelBase
{
    public string Name { get; }
    public object? Value { get; }

    public CompositionTreeSnapshotItemPropertyViewModel(string name, object? value)
    {
        Name = name;
        Value = value;
    }
}