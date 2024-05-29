using System;
using System.Text;
using Avalonia.VisualTree;

namespace Avalonia.Input;

public class PointerGesture : IEquatable<PointerGesture>
{
    public PointerGesture(MouseButton button, KeyModifiers modifiers = KeyModifiers.None)
    {
        Button = button;
        KeyModifiers = modifiers;
    }

    public bool Equals(PointerGesture? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        return Button == other.Button && KeyModifiers == other.KeyModifiers;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;

        return obj is KeyGesture gesture && Equals(gesture);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((int)Button * 397) ^ (int)KeyModifiers;
        }
    }

    public static bool operator ==(PointerGesture? left, PointerGesture? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(PointerGesture? left, PointerGesture? right)
    {
        return !Equals(left, right);
    }

    public MouseButton Button { get; }

    public KeyModifiers KeyModifiers { get; }

    public override string ToString()
    {
        var s = new StringBuilder();

        static void Plus(StringBuilder s)
        {
            if (s.Length > 0)
            {
                s.Append("+");
            }
        }

        if (KeyModifiers.HasAllFlags(KeyModifiers.Control))
        {
            s.Append("Ctrl");
        }

        if (KeyModifiers.HasAllFlags(KeyModifiers.Shift))
        {
            Plus(s);
            s.Append("Shift");
        }

        if (KeyModifiers.HasAllFlags(KeyModifiers.Alt))
        {
            Plus(s);
            s.Append("Alt");
        }

        if (KeyModifiers.HasAllFlags(KeyModifiers.Meta))
        {
            Plus(s);
            s.Append("Cmd");
        }

        Plus(s);
        s.Append(Button);

        return s.ToString();
    }

    public bool Matches(PointerEventArgs pointerEvent) =>
        pointerEvent != null &&
        pointerEvent.KeyModifiers == KeyModifiers &&
        pointerEvent.GetCurrentPoint(pointerEvent.Source as Visual).Properties.PointerUpdateKind.GetMouseButton() == Button;

    public bool Matches(PointerReleasedEventArgs pointerEvent) =>
        pointerEvent != null &&
        pointerEvent.KeyModifiers == KeyModifiers &&
        pointerEvent.InitialPressMouseButton == Button;
}
