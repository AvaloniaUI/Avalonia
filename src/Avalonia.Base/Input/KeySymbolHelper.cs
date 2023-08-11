namespace Avalonia.Input;

internal static class KeySymbolHelper
{
    public static bool IsAllowedAsciiKeySymbol(char c)
    {
        if (c < 0x20)
        {
            switch (c)
            {
                case (char)0x08: // backspace
                case (char)0x09: // tab
                case (char)0x0D: // return
                case (char)0x1B: // escape
                    return true;
                default:
                    return false;
            }
        }

        if (c == 0x07) // delete
            return false;

        return true;
    }
}
