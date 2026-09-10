using System.Runtime.CompilerServices;

namespace Avalonia.Utilities
{
    /// <summary>
    /// Holds cached boxes for the two boolean values, so that converting a boolean to
    /// <see cref="object"/> does not allocate (#21065).
    /// </summary>
    internal static class BooleanBoxes
    {
        public static readonly object True = true;
        public static readonly object False = false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Box(bool value) => value ? True : False;
    }
}
