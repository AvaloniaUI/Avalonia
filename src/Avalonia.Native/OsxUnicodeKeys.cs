using System.Collections.Generic;
using Avalonia.Input;

namespace Avalonia.Native.Interop
{
    internal static class OsxUnicodeKeys
    {
        enum OsxUnicodeSpecialKey
        {
            NSUpArrowFunctionKey = 0xF700,
            NSDownArrowFunctionKey = 0xF701,
            NSLeftArrowFunctionKey = 0xF702,
            NSRightArrowFunctionKey = 0xF703,
            NSF1FunctionKey = 0xF704,
            NSF2FunctionKey = 0xF705,
            NSF3FunctionKey = 0xF706,
            NSF4FunctionKey = 0xF707,
            NSF5FunctionKey = 0xF708,
            NSF6FunctionKey = 0xF709,
            NSF7FunctionKey = 0xF70A,
            NSF8FunctionKey = 0xF70B,
            NSF9FunctionKey = 0xF70C,
            NSF10FunctionKey = 0xF70D,
            NSF11FunctionKey = 0xF70E,
            NSF12FunctionKey = 0xF70F,
            NSF13FunctionKey = 0xF710,
            NSF14FunctionKey = 0xF711,
            NSF15FunctionKey = 0xF712,
            NSF16FunctionKey = 0xF713,
            NSF17FunctionKey = 0xF714,
            NSF18FunctionKey = 0xF715,
            NSF19FunctionKey = 0xF716,
            NSF20FunctionKey = 0xF717,
            NSF21FunctionKey = 0xF718,
            NSF22FunctionKey = 0xF719,
            NSF23FunctionKey = 0xF71A,
            NSF24FunctionKey = 0xF71B,
            NSF25FunctionKey = 0xF71C,
            NSF26FunctionKey = 0xF71D,
            NSF27FunctionKey = 0xF71E,
            NSF28FunctionKey = 0xF71F,
            NSF29FunctionKey = 0xF720,
            NSF30FunctionKey = 0xF721,
            NSF31FunctionKey = 0xF722,
            NSF32FunctionKey = 0xF723,
            NSF33FunctionKey = 0xF724,
            NSF34FunctionKey = 0xF725,
            NSF35FunctionKey = 0xF726,
            NSInsertFunctionKey = 0xF727,
            NSDeleteFunctionKey = 0xF728,
            NSHomeFunctionKey = 0xF729,
            NSBeginFunctionKey = 0xF72A,
            NSEndFunctionKey = 0xF72B,
            NSPageUpFunctionKey = 0xF72C,
            NSPageDownFunctionKey = 0xF72D,
            NSPrintScreenFunctionKey = 0xF72E,
            NSScrollLockFunctionKey = 0xF72F,
            NSPauseFunctionKey = 0xF730,
            NSSysReqFunctionKey = 0xF731,
            NSBreakFunctionKey = 0xF732,
            NSResetFunctionKey = 0xF733,
            NSStopFunctionKey = 0xF734,
            NSMenuFunctionKey = 0xF735,
            NSUserFunctionKey = 0xF736,
            NSSystemFunctionKey = 0xF737,
            NSPrintFunctionKey = 0xF738,
            NSClearLineFunctionKey = 0xF739,
            NSClearDisplayFunctionKey = 0xF73A,
            NSInsertLineFunctionKey = 0xF73B,
            NSDeleteLineFunctionKey = 0xF73C,
            NSInsertCharFunctionKey = 0xF73D,
            NSDeleteCharFunctionKey = 0xF73E,
            NSPrevFunctionKey = 0xF73F,
            NSNextFunctionKey = 0xF740,
            NSSelectFunctionKey = 0xF741,
            NSExecuteFunctionKey = 0xF742,
            NSUndoFunctionKey = 0xF743,
            NSRedoFunctionKey = 0xF744,
            NSFindFunctionKey = 0xF745,
            NSHelpFunctionKey = 0xF746,
            NSModeSwitchFunctionKey = 0xF747
        }

        private static Dictionary<Key, OsxUnicodeSpecialKey> s_osxKeys = new Dictionary<Key, OsxUnicodeSpecialKey>
        {
            {Key.Up, OsxUnicodeSpecialKey.NSUpArrowFunctionKey },
            {Key.Down, OsxUnicodeSpecialKey.NSDownArrowFunctionKey },
            {Key.Left, OsxUnicodeSpecialKey.NSLeftArrowFunctionKey },
            {Key.Right, OsxUnicodeSpecialKey.NSRightArrowFunctionKey },
            { Key.F1, OsxUnicodeSpecialKey.NSF1FunctionKey },
            { Key.F2, OsxUnicodeSpecialKey.NSF2FunctionKey },
            { Key.F3, OsxUnicodeSpecialKey.NSF3FunctionKey },
            { Key.F4, OsxUnicodeSpecialKey.NSF4FunctionKey },
            { Key.F5, OsxUnicodeSpecialKey.NSF5FunctionKey },
            { Key.F6, OsxUnicodeSpecialKey.NSF6FunctionKey },
            { Key.F7, OsxUnicodeSpecialKey.NSF7FunctionKey },
            { Key.F8, OsxUnicodeSpecialKey.NSF8FunctionKey },
            { Key.F9, OsxUnicodeSpecialKey.NSF9FunctionKey },
            { Key.F10, OsxUnicodeSpecialKey.NSF10FunctionKey },
            { Key.F11, OsxUnicodeSpecialKey.NSF11FunctionKey },
            { Key.F12, OsxUnicodeSpecialKey.NSF12FunctionKey },
            { Key.F13, OsxUnicodeSpecialKey.NSF13FunctionKey },
            { Key.F14, OsxUnicodeSpecialKey.NSF14FunctionKey },
            { Key.F15, OsxUnicodeSpecialKey.NSF15FunctionKey },
            { Key.F16, OsxUnicodeSpecialKey.NSF16FunctionKey },
            { Key.F17, OsxUnicodeSpecialKey.NSF17FunctionKey },
            { Key.F18, OsxUnicodeSpecialKey.NSF18FunctionKey },
            { Key.F19, OsxUnicodeSpecialKey.NSF19FunctionKey },
            { Key.F20, OsxUnicodeSpecialKey.NSF20FunctionKey },
            { Key.F21, OsxUnicodeSpecialKey.NSF21FunctionKey },
            { Key.F22, OsxUnicodeSpecialKey.NSF22FunctionKey },
            { Key.F23, OsxUnicodeSpecialKey.NSF23FunctionKey },
            { Key.F24, OsxUnicodeSpecialKey.NSF24FunctionKey },
            { Key.Insert, OsxUnicodeSpecialKey.NSInsertFunctionKey },
            { Key.Delete, OsxUnicodeSpecialKey.NSDeleteFunctionKey },
            { Key.Home, OsxUnicodeSpecialKey.NSHomeFunctionKey },
            //{ Key.Begin, OsxUnicodeSpecialKey.NSBeginFunctionKey },
            { Key.End, OsxUnicodeSpecialKey.NSEndFunctionKey },
            { Key.PageUp, OsxUnicodeSpecialKey.NSPageUpFunctionKey },
            { Key.PageDown, OsxUnicodeSpecialKey.NSPageDownFunctionKey },
            { Key.PrintScreen, OsxUnicodeSpecialKey.NSPrintScreenFunctionKey },
            { Key.Scroll, OsxUnicodeSpecialKey.NSScrollLockFunctionKey },
            //{ Key.SysReq, OsxUnicodeSpecialKey.NSSysReqFunctionKey },
            //{ Key.Break, OsxUnicodeSpecialKey.NSBreakFunctionKey },
            //{ Key.Reset, OsxUnicodeSpecialKey.NSResetFunctionKey },
            //{ Key.Stop, OsxUnicodeSpecialKey.NSStopFunctionKey },
            //{ Key.Menu, OsxUnicodeSpecialKey.NSMenuFunctionKey },
            //{ Key.UserFunction, OsxUnicodeSpecialKey.NSUserFunctionKey },
            //{ Key.SystemFunction, OsxUnicodeSpecialKey.NSSystemFunctionKey },
            { Key.Print, OsxUnicodeSpecialKey.NSPrintFunctionKey },
            //{ Key.ClearLine, OsxUnicodeSpecialKey.NSClearLineFunctionKey },
            //{ Key.ClearDisplay, OsxUnicodeSpecialKey.NSClearDisplayFunctionKey },
        };

        public static string ConvertOSXSpecialKeyCodes(Key key)
        {
            if (s_osxKeys.ContainsKey(key))
            {
                return ((char)s_osxKeys[key]).ToString();
            }
            else
            {
                if (key >= Key.D0 && key <= Key.D9)
                {
                    return key.ToString().Replace("D", "");
                }
                
                return key.ToString().ToLower();
            }
        }
    }
}
