using System;
using System.Globalization;
using System.Linq;
using Avalonia.Utilities;

namespace Avalonia.Input;

/// <summary>
/// Defines a pointer input combination.
/// </summary>
public record PointerActionGesture(MouseButton Button, KeyModifiers KeyModifiers = KeyModifiers.None, int ClickCount = 1)
{
    private const string ButtonNameSuffixSingle = "Click";
    private const string ButtonNameSuffixDouble = "Double" + ButtonNameSuffixSingle;
    
    public virtual bool Equals(PointerActionGesture? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        
        return Button == other.Button && KeyModifiers == other.KeyModifiers && ClickCount == other.ClickCount;
    }
    
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (int)Button;
            hashCode = (hashCode * 397) ^ (int)KeyModifiers;
            hashCode = (hashCode * 397) ^ ClickCount;
            return hashCode;
        }
    }
    
    public override string ToString() => ToString(null, null);

    /// <summary>
    /// Returns the current PointerGesture as a string formatted according to the format string and appropriate IFormatProvider
    /// </summary>
    /// <param name="format">The format to use. 
    /// <list type="bullet">
    /// <item><term>null or "" or "g"</term><description>The Invariant format, uses Enum.ToString() to format Keys.</description></item>
    /// <item><term>"p"</term><description>Use platform specific formatting as registerd.</description></item>
    /// </list></param>
    /// <param name="formatProvider">The IFormatProvider to use.  If null, uses the appropriate provider registered in the Avalonia Locator, or Invariant.</param>
    /// <returns>The formatted string.</returns>
    /// <exception cref="FormatException">Thrown if the format string is not null, "", "g", or "p"</exception>
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        KeyGesture.FormatWithKeyModifiers(_ => ClickCount switch
        {
            1 => Button + ButtonNameSuffixSingle,
            2 => Button + ButtonNameSuffixDouble,
            _ => Button + ButtonNameSuffixSingle + "*" + ClickCount.ToString(CultureInfo.InvariantCulture)
        }, KeyModifiers, format, formatProvider);

    public static PointerActionGesture Parse(string gesture)
    {
        var parts = gesture.Split('+');
        var modifiers = parts.Take(parts.Length - 1).Select(x => KeyGesture.ParseModifier(x.AsSpan())).Aggregate((x, y) => x | y);
        var buttonName = parts.Last();

        var clickCount = 1;
        if (buttonName.EndsWith(ButtonNameSuffixDouble, StringComparison.OrdinalIgnoreCase))
        {
            buttonName = buttonName.Substring(0, buttonName.Length - ButtonNameSuffixDouble.Length);
            clickCount = 2;
        }
        else 
        {
            var countParts = buttonName.Split('*');
            buttonName = countParts[0];
            if (countParts.Length > 1)
                clickCount = int.Parse(countParts[1], CultureInfo.InvariantCulture);
            if (buttonName.EndsWith(ButtonNameSuffixSingle, StringComparison.OrdinalIgnoreCase))
                buttonName = buttonName.Substring(0, buttonName.Length - ButtonNameSuffixSingle.Length);
        }
        
        var button =  EnumHelper.Parse<MouseButton>(buttonName, true);
        return new PointerActionGesture(button, modifiers, clickCount);
    }

    public bool Matches(PointerEventArgs pointerEvent) =>
        pointerEvent != null &&
        pointerEvent.KeyModifiers == KeyModifiers &&
        pointerEvent.GetCurrentPoint(pointerEvent.Source as Visual).Properties.PointerUpdateKind.GetMouseButton() == Button;

    public bool Matches(PointerPressedEventArgs pointerEvent) =>
        pointerEvent != null &&
        pointerEvent.ClickCount == ClickCount &&
        pointerEvent.KeyModifiers == KeyModifiers &&
        pointerEvent.GetCurrentPoint(pointerEvent.Source as Visual).Properties.PointerUpdateKind.GetMouseButton() == Button;

    public bool Matches(PointerReleasedEventArgs pointerEvent) =>
        pointerEvent != null &&
        pointerEvent.KeyModifiers == KeyModifiers &&
        pointerEvent.InitialPressMouseButton == Button;
}
