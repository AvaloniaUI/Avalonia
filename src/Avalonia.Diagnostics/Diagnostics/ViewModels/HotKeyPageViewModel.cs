using System.Collections.ObjectModel;

namespace Avalonia.Diagnostics.ViewModels;

internal record HotKeyDescription(string Description, string LongDescription, string Gesture);
internal class HotKeyPageViewModel : ViewModelBase
{
    public ObservableCollection<HotKeyDescription> HotKeys { get; } = new();

    public HotKeyPageViewModel()
    {
        HotKeys = new()
        {
            new("Enable Snapshot Frames", "Pauses refreshing the Value Frames inspector for the selected Control", "Alt+S"),
            new("Disable Snapshot Frames", "Resumes refreshing the Value Frames inspector for the selected Control", "Alt+D"),
            new("Inspect Control Under Pointer", "Inspects the hovered Control in the Logical or Visual Tree Page", "Ctrl+Shift"),
            new("Toggle Popup Freeze", "Prevents visible Popups from closing so they can be inspected", "Ctrl+Alt+F"),
            new("Screenshot Selected Control", "Saves a Screenshot of the Selected Control in the Logical or Visual Tree Page", "F8")
        };
    }
}
