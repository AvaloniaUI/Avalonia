using System;
using System.Collections.Generic;
using Avalonia.FreeDesktop.DBusIme.Fcitx;
using Tmds.DBus;

namespace Avalonia.FreeDesktop.DBusIme
{
    public class X11DBusImeHelper
    {
        private static readonly Dictionary<string, Func<Connection, IX11InputMethodFactory>> KnownMethods =
            new Dictionary<string, Func<Connection, IX11InputMethodFactory>>
            {
                ["fcitx"] = conn => new FcitxIx11TextInputMethodFactory(conn)
            };
        
        static bool IsCjkLocale(string lang)
        {
            if (lang == null)
                return false;
            return lang.Contains("zh")
                   || lang.Contains("ja")
                   || lang.Contains("vi")
                   || lang.Contains("ko");
        }

        static Func<Connection, IX11InputMethodFactory> DetectInputMethod()
        {
            foreach (var name in new[] { "AVALONIA_IM_MODULE", "GTK_IM_MODULE", "QT_IM_MODULE" })
            {
                var value = Environment.GetEnvironmentVariable(name);
                if (value != null && KnownMethods.TryGetValue(value, out var factory))
                    return factory;
            }

            return null;
        }
        
        public static void RegisterIfNeeded(bool? optionsWantIme)
        {
            if(
                optionsWantIme == true
                || Environment.GetEnvironmentVariable("AVALONIA_FORCE_IME") == "1"
                || (optionsWantIme == null && IsCjkLocale(Environment.GetEnvironmentVariable("LANG"))))
            {
                var factory = DetectInputMethod();
                if (factory != null)
                {
                    var conn = DBusHelper.TryInitialize();
                    if (conn != null)
                        AvaloniaLocator.CurrentMutable.Bind<IX11InputMethodFactory>().ToConstant(factory(conn));
                }
            }
        }
    }
}
