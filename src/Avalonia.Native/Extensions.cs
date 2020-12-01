namespace Avalonia.Native
{
    internal static class Extensions
    {
        public static int AsComBool(this bool b) => b ? 1 : 0;
        public static bool FromComBool(this int b) => b != 0;
    }
}
