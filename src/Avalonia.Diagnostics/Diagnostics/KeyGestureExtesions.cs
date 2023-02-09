using Avalonia.Input;
using Avalonia.Input.Raw;

namespace Avalonia.Diagnostics
{
    internal static class KeyGestureExtesions
    {
        public static bool Matches(this KeyGesture gesture, RawKeyEventArgs keyEvent) =>
            (KeyModifiers)(keyEvent.Modifiers & RawInputModifiers.KeyboardMask) == gesture.KeyModifiers &&
                ResolveNumPadOperationKey(keyEvent.Key) == ResolveNumPadOperationKey(gesture.Key);

        private static Key ResolveNumPadOperationKey(Key key)
        {
            switch (key)
            {
                case Key.Add:
                    return Key.OemPlus;
                case Key.Subtract:
                    return Key.OemMinus;
                case Key.Decimal:
                    return Key.OemPeriod;
                default:
                    return key;
            }
        }
    }
}
