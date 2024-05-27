using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Input.Platform
{
    public sealed class KeyGestureFormatInfo(Dictionary<Key, string>? platformKeyOverrides = null,
                                             string meta = "Cmd",
                                             string ctrl = "Ctrl",
                                             string alt = "Alt",
                                             string shift = "Shift") : IFormatProvider
    {
        public static KeyGestureFormatInfo Invariant { get; } = new();

        public string Meta { get; } = meta;

        public string Ctrl { get; } = ctrl;

        public string Alt { get; } = alt;

        public string Shift { get; } = shift;

        public object? GetFormat(Type? formatType) => formatType == typeof(KeyGestureFormatInfo) ? this : null;
        
        public static KeyGestureFormatInfo GetInstance(IFormatProvider? formatProvider)
            => formatProvider?.GetFormat(typeof(KeyGestureFormatInfo)) as KeyGestureFormatInfo
            ?? AvaloniaLocator.Current.GetService<KeyGestureFormatInfo>()
            ?? Invariant;

        /* A dictionary of the common platform Key overrides. These are used as a fallback
         * if platformKeyOverrides doesn't contain the Key in question.
         */
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
            { Key.Subtract , "-" }
        };

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
