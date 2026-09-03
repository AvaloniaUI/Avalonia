using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using MiniMvvm;

namespace ControlCatalog.ViewModels;

public class WrapPanelPageViewModel : ViewModelBase
{
    public WrapPanelItemsAlignment[] ItemsAlignments { get; } = Enum.GetValues<WrapPanelItemsAlignment>();

    public Orientation[] Orientations { get; } = Enum.GetValues<Orientation>();

    public IReadOnlyList<WrapPanelItemViewModel> Items { get; } = CreateItems();

    public WrapPanelItemsAlignment ItemsAlignment
    {
        get;
        set => RaiseAndSetIfChanged(ref field, value);
    } = WrapPanelItemsAlignment.Start;

    public Orientation Orientation
    {
        get;
        set => RaiseAndSetIfChanged(ref field, value);
    } = Orientation.Horizontal;

    public double ItemSpacing
    {
        get;
        set => RaiseAndSetIfChanged(ref field, value);
    } = 5;

    public double LineSpacing
    {
        get;
        set => RaiseAndSetIfChanged(ref field, value);
    } = 5;

    public bool IsItemWidthEnabled
    {
        get;
        set
        {
            if (RaiseAndSetIfChanged(ref field, value))
                RaisePropertyChanged(nameof(EffectiveItemWidth));
        }
    }

    public double ItemWidthValue
    {
        get;
        set
        {
            if (RaiseAndSetIfChanged(ref field, value))
                RaisePropertyChanged(nameof(EffectiveItemWidth));
        }
    } = 140;

    public bool IsItemHeightEnabled
    {
        get;
        set
        {
            if (RaiseAndSetIfChanged(ref field, value))
                RaisePropertyChanged(nameof(EffectiveItemHeight));
        }
    }

    public double ItemHeightValue
    {
        get;
        set
        {
            if (RaiseAndSetIfChanged(ref field, value))
                RaisePropertyChanged(nameof(EffectiveItemHeight));
        }
    } = 90;

    public double EffectiveItemWidth
        => IsItemWidthEnabled ? ItemWidthValue : double.NaN;

    public double EffectiveItemHeight
        => IsItemHeightEnabled ? ItemHeightValue : double.NaN;

    private static WrapPanelItemViewModel[] CreateItems()
    {
        var random = new Random(42);
        var items = new WrapPanelItemViewModel[50];

        for (var i = 0; i < items.Length; ++i)
            items[i] = new WrapPanelItemViewModel(i + 1, new Thickness(random.Next(15, 56), random.Next(8, 31)));

        return items;
    }
}

public class WrapPanelItemViewModel(int number, Thickness padding)
{
    public int Number { get; } = number;

    public Thickness Padding { get; } = padding;
}
