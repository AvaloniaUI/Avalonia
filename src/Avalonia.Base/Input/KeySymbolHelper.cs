namespace Avalonia.Input;

internal static class KeySymbolHelper
{
    public static bool IsAllowedAsciiKeySymbol(char c)
    {
        if (c < 0x20)
        {
            switch (c)
            {
                case '\b': // backspace
                case '\t': // tab
                case '\r': // return
                case (char)0x1B: // escape
                    return true;
                default:
                    return false;
            }
        }

        if (c == 0x7F) // delete
            return false;

        return true;
    }
}
