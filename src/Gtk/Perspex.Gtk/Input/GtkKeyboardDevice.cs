





namespace Perspex.Gtk
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Perspex.Input;
    
    public class GtkKeyboardDevice : KeyboardDevice
    {
        private static GtkKeyboardDevice instance;
        private static readonly Dictionary<Gdk.Key, string> NameDic = new Dictionary<Gdk.Key, string>();

        static GtkKeyboardDevice()
        {
            instance = new GtkKeyboardDevice();
            foreach (var f in typeof (Gdk.Key).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var key = (Gdk.Key) f.GetValue(null);
                if(NameDic.ContainsKey(key))
                    continue;
                NameDic[key] = f.Name;
            }
        }

        private GtkKeyboardDevice()
        {
        }

        public static new GtkKeyboardDevice Instance
        {
            get { return instance; }
        }

        public static Perspex.Input.Key ConvertKey(Gdk.Key key)
        {
            // TODO: Don't use reflection for this! My eyes!!!
            if (key == Gdk.Key.BackSpace)
            {
                return Perspex.Input.Key.Back;
            }
            else
            {
                string s;
                if (!NameDic.TryGetValue(key, out s))
                    s = "Unknown";
                Perspex.Input.Key result;

                if (Enum.TryParse(s, true, out result))
                {
                    return result;
                }
                else
                {
                    return Perspex.Input.Key.None;
                }
            }
        }
    }
}