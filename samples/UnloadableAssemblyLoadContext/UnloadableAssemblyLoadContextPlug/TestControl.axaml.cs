using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace UnloadableAssemblyLoadContextPlug;

public class TestControl : TemplatedControl
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<TestControl, string>(nameof(Text), "Plugin control");

    public static readonly DirectProperty<TestControl, int> CountProperty =
        AvaloniaProperty.RegisterDirect<TestControl, int>(nameof(Count), control => control.Count,
            (control, value) => control.Count = value);

    public static readonly AttachedProperty<int> PriorityProperty =
        AvaloniaProperty.RegisterAttached<TestControl, Control, int>("Priority", inherits: true);

    private int _count;

    static TestControl()
    {
        AffectsMeasure<TestControl>(TextProperty);
        AffectsRender<TestControl>(CountProperty);
        MaxWidthProperty.OverrideDefaultValue<TestControl>(400);
    }

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public int Count
    {
        get => _count;
        set => SetAndRaise(CountProperty, ref _count, value);
    }

    public static int GetPriority(Control control) => control.GetValue(PriorityProperty);
    public static void SetPriority(Control control, int value) => control.SetValue(PriorityProperty, value);
}

