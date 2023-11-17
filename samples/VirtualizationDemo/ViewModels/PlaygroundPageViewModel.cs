using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using MiniMvvm;

namespace VirtualizationDemo.ViewModels;

public class PlaygroundPageViewModel : ViewModelBase
{
    private SelectionMode _selectionMode = SelectionMode.Multiple;
    private int _scrollToIndex = 500;
    private string? _newItemHeader = "New Item 1";

    public PlaygroundPageViewModel()
    {
        Items = new(Enumerable.Range(0, 1000).Select(x => new PlaygroundItemViewModel(x)));
        Selection = new();
    }

    public ObservableCollection<PlaygroundItemViewModel> Items { get; }

    public bool Multiple
    {
        get => _selectionMode.HasFlag(SelectionMode.Multiple);
        set => SetSelectionMode(SelectionMode.Multiple, value);
    }

    public bool Toggle
    {
        get => _selectionMode.HasFlag(SelectionMode.Toggle);
        set => SetSelectionMode(SelectionMode.Toggle, value);
    }

    public bool AlwaysSelected
    {
        get => _selectionMode.HasFlag(SelectionMode.AlwaysSelected);
        set => SetSelectionMode(SelectionMode.AlwaysSelected, value);
    }

    public SelectionModel<PlaygroundItemViewModel> Selection { get; }
    
    public SelectionMode SelectionMode
    {
        get => _selectionMode;
        set => RaiseAndSetIfChanged(ref _selectionMode, value);
    }

    public int ScrollToIndex
    {
        get => _scrollToIndex;
        set => RaiseAndSetIfChanged(ref _scrollToIndex, value);
    }

    public string? NewItemHeader
    {
        get => _newItemHeader;
        set => RaiseAndSetIfChanged(ref _newItemHeader, value);
    }

    public void ExecuteScrollToIndex()
    {
        Selection.Select(ScrollToIndex);
    }

    public void RandomizeScrollToIndex()
    {
        var rnd = new Random();
        ScrollToIndex = rnd.Next(Items.Count);
    }

    public void AddAtSelectedIndex()
    {
        if (Selection.SelectedIndex == -1)
            return;
        Items.Insert(Selection.SelectedIndex, new(NewItemHeader));
    }

    public void DeleteSelectedItem()
    {
        var count = Selection.Count;
        for (var i = count - 1; i >= 0; i--)
            Items.RemoveAt(Selection.SelectedIndexes[i]);
    }

    private void SetSelectionMode(SelectionMode mode, bool value)
    {
        if (value)
            SelectionMode |= mode;
        else
            SelectionMode &= ~mode;
    }
}
