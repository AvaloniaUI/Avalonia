using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Input.Platform
{

    /// <summary>
    /// Provides platform specific formatting information for the KeyGesture class
    /// </summary>
    /// <param name="platformKeyOverrides">A dictionary of Key to String overrides for specific characters, for example Key.Left to "Left Arrow" or "←" on Mac.
    /// A null value is assumed to be the Invariant, so the included set of common overrides will be skipped if this is null.  If only the common overrides are
    /// desired, pass an empty Dictionary instead.</param>
    /// <param name="meta">The string to use for the Meta modifier, defaults to "Cmd"</param>
    /// <param name="ctrl">The string to use for the Ctrl modifier, defaults to "Ctrl"</param>
    /// <param name="alt">The string to use for the Alt modifier, defaults to "Alt"</param>
    /// <param name="shift">The string to use for the Shift modifier, defaults to "Shift"</param>
    public sealed class KeyGestureFormatInfo(IReadOnlyDictionary<Key, string>? platformKeyOverrides = null,
                                             string meta = "Cmd",
                                             string ctrl = "Ctrl",
                                             string alt = "Alt",
                                             string shift = "Shift") : IFormatProvider
    {
        /// <summary>
        /// The Invariant format.  Only uses strings straight from the appropriate Enums.
        /// </summary>
        public static KeyGestureFormatInfo Invariant { get; } = new();

        /// <summary>
        /// The string used to represent Meta on the appropriate platform.  Defaults to "Cmd".
        /// </summary>
        public string Meta { get; } = meta;
        
        /// <summary>
        /// The string used to represent Ctrl on the appropriate platform.  Defaults to "Ctrl".
        /// </summary>
        public string Ctrl { get; } = ctrl;
        
        /// <summary>
        /// The string used to represent Alt on the appropriate platform.  Defaults to "Alt".
        /// </summary>
        public string Alt { get; } = alt;
       
        /// <summary>
        /// The string used to represent Shift on the appropriate platform.  Defaults to "Shift".
        /// </summary>
        public string Shift { get; } = shift;

        public object? GetFormat(Type? formatType) => formatType == typeof(KeyGestureFormatInfo) ? this : null;
        
        /// <summary>
        /// Gets the most appropriate KeyGestureFormatInfo for the IFormatProvider requested.  This will be, in order:
        /// 1. The provided IFormatProvider as a KeyGestureFormatInfo
        /// 2. The currently registered platform specific KeyGestureFormatInfo, if present.
        /// 3. The Invariant otherwise.
        /// </summary>
        /// <param name="formatProvider">The IFormatProvider to get a KeyGestureFormatInfo for.</param>
        /// <returns></returns>
        public static KeyGestureFormatInfo GetInstance(IFormatProvider? formatProvider)
            => formatProvider?.GetFormat(typeof(KeyGestureFormatInfo)) as KeyGestureFormatInfo
            ?? AvaloniaLocator.Current.GetService<KeyGestureFormatInfo>()
            ?? Invariant;

        /// <summary>
        /// A dictionary of the common platform Key overrides. These are used as a fallback
        /// if platformKeyOverrides doesn't contain the Key in question.
        /// </summary>
        
        private static readonly Dictionary<Key, string> s_commonKeyOverrides = new()
        {
            { Key.Add , "+" },
            { Key.D0 , "0" },
            { Key.D1 , "1" },
            { Key.D2 , "2" },
            { Key.D3 , "3" },
            { Key.D4 , "4" },
            { Key.D5 , "5" },
            { Key.D6 , "6" },
            { Key.D7 , "7" },
            { Key.D8 , "8" },
            { Key.D9 , "9" },
            { Key.Decimal , "." },
            { Key.Divide , "/" },
            { Key.Multiply , "*" },
            { Key.OemBackslash , "\\" },
            { Key.OemCloseBrackets , "]" },
            { Key.OemComma , "," },
            { Key.OemMinus , "-" },
            { Key.OemOpenBrackets , "[" },
            { Key.OemPeriod , "." },
            { Key.OemPipe , "|" },
            { Key.OemPlus , "+" },
            { Key.OemQuestion , "/" },
            { Key.OemQuotes , "\"" },
            { Key.OemSemicolon , ";" },
            { Key.OemTilde , "`" },
            { Key.Separator , "/" },
            { Key.Subtract , "-" },
            { Key.Back , "Backspace" },
            { Key.Down , "Down Arrow" },
            { Key.Left , "Left Arrow" },
            { Key.Right , "Right Arrow" },
            { Key.Up , "Up Arrow" }
        };

        /// <summary>
        /// Checks the platformKeyOverrides and s_commonKeyOverrides Dictionaries, in order, for the appropriate
        /// string to represent the given Key on this platform.
        /// NOTE: If platformKeyOverrides is null, this is assumed to be the Invariant and the Dictionaries are not checked.
        /// The plain Enum string is returned instead.
        /// </summary>
        /// <param name="key">The Key to format.</param>
        /// <returns>The appropriate platform specific or common override if present, key.ToString() if not or this is the Invariant.</returns>
        public string FormatKey(Key key)
        {
            /*
             * The absence of an Overrides dictionary indicates this is the invariant, and
             * so should just return the default ToString() value.
             */
            if (platformKeyOverrides == null)
                return key.ToString();

            return platformKeyOverrides.TryGetValue(key, out string? result) ? result : 
                   s_commonKeyOverrides.TryGetValue(key, out string? cresult) ? cresult :
                   key.ToString() ;
            
        }

        
    }
}
