using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Styling;

namespace Avalonia.Diagnostics.Controls.VirtualizedTreeView;

internal class VirtualizedTreeView : TemplatedControl
{
    private FlatTree _source = new(Array.Empty<ITreeNode>());

    public static readonly DirectProperty<VirtualizedTreeView, FlatTree> SourceProperty =
        AvaloniaProperty.RegisterDirect<VirtualizedTreeView, FlatTree>(nameof(Source),
            o => o.Source);

    /// <summary>
    /// Defines the <see cref="ItemsSource"/> property.
    /// </summary>
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<VirtualizedTreeView, IEnumerable?>(nameof(ItemsSource));

    /// <summary>
    /// Defines the <see cref="ItemTemplate"/> property.
    /// </summary>
    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<VirtualizedTreeView, IDataTemplate?>(nameof(ItemTemplate));

    /// <summary>
    /// Defines the <see cref="SelectedItem"/> property.
    /// </summary>
    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<VirtualizedTreeView, object?>(nameof(SelectedItem),
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// Defines the <see cref="ItemContainerTheme"/> property.
    /// </summary>
    public static readonly StyledProperty<ControlTheme?> ItemContainerThemeProperty =
        AvaloniaProperty.Register<VirtualizedTreeView, ControlTheme?>(nameof(ItemContainerTheme));

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public ControlTheme? ItemContainerTheme
    {
        get => GetValue(ItemContainerThemeProperty);
        set => SetValue(ItemContainerThemeProperty, value);
    }

    public FlatTree Source
    {
        get { return _source; }
        set { SetAndRaise(SourceProperty, ref _source, value); }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemsSourceProperty)
        {
            var oldSource = Source;
            FlatTree newSource;
            if (ItemsSource is IEnumerable<ITreeNode> sourceNodes)
            {
                newSource = new FlatTree(sourceNodes);
            }
            else
            {
                newSource = new FlatTree(Array.Empty<ITreeNode>());
            }
            Source = newSource;
            RaisePropertyChanged(SourceProperty, oldSource, newSource);
        }
    }
}
