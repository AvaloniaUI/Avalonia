using System.Collections.Generic;

namespace Avalonia.Markup.Xaml.UnitTests;

public class DependsOnControlTest : Visual
{
    public DependsOnControlTest()
    {
        
    }

    private int _second;
    private readonly List<PropertyChangeLogEntry> _changes = new();

    public static readonly StyledProperty<int> FirstProperty =
        AvaloniaProperty.Register<DependsOnControlTest, int>(nameof(First));

    public int First
    {
        get => GetValue(FirstProperty);
        set => SetValue(FirstProperty, value);
    }

    public static readonly DirectProperty<DependsOnControlTest, int> SecondProperty =
        AvaloniaProperty.RegisterDirect<DependsOnControlTest, int>(nameof(Second),
            o => o.Second,
            (o, v) => o.Second = v);

    [Metadata.DependsOn(nameof(First))]
    public int Second
    {
        get => _second;
        set => SetAndRaise(SecondProperty, ref _second, value);
    }

    internal IReadOnlyList<PropertyChangeLogEntry> Changes => _changes;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        _changes.Add(new(change.Property.Name, change.NewValue, change.OldValue));
    }
}
