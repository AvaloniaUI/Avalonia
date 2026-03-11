using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace ControlCatalog;

public class CarbonEmissionsHack
{
    private static readonly AttachedProperty<bool> IsSubscribedProperty =
        AvaloniaProperty.RegisterAttached<CarbonEmissionsHack, Control, bool>("IsSubscribed");

    public static void SetTracksVisualTreeAttachment(Control control, bool value)
    {
        if (!value)
            return;

        if (control.GetValue(IsSubscribedProperty))
            return;
        control.SetValue(IsSubscribedProperty, true);

        const string className = "is-attached-to-visual-tree";
        if (control.IsAttachedToVisualTree())
            control.Classes.Add(className);
        control.AttachedToVisualTree += delegate
        {
            control.Classes.Add(className);
        };
        control.DetachedFromVisualTree += delegate
        {
            control.Classes.Remove(className);
        };
    }
}