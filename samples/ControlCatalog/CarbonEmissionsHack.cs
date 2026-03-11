using Avalonia.Controls;
using Avalonia.VisualTree;

namespace ControlCatalog;

public class CarbonEmissionsHack
{
    public static void SetTracksVisualTreeAttachment(Control control, bool value)
    {
        if (!value)
            return;

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