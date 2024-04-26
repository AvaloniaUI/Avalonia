namespace Avalonia.Controls;

/// <summary>
/// Defines how a <see cref="MenuItem"/> or <see cref="NativeMenuItem"/> reacts to clicks.
/// </summary>
public enum MenuItemToggleType
{
    /// <summary>
    /// Normal menu item.
    /// </summary>
    None,
    
    /// <summary>
    /// Toggleable menu item with a checkbox.
    /// </summary>
    CheckBox,
    
    /// <summary>
    /// Menu item representing single option of radio group. 
    /// </summary>
    Radio
}
