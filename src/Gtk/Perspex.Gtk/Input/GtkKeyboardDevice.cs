// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Perspex.Input;

namespace Perspex.Gtk
{
    public class GtkKeyboardDevice : KeyboardDevice
    {
        private static GtkKeyboardDevice s_instance;
        private static readonly Dictionary<Gdk.Key, string> s_nameDic = new Dictionary<Gdk.Key, string>();

        static GtkKeyboardDevice()
        {
            s_instance = new GtkKeyboardDevice();
            foreach (var f in typeof(Gdk.Key).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var key = (Gdk.Key)f.GetValue(null);
                if (s_nameDic.ContainsKey(key))
                    continue;
                s_nameDic[key] = f.Name;
            }
        }

        private GtkKeyboardDevice()
        {
        }

        public static new GtkKeyboardDevice Instance => s_instance;

        public static Key ConvertKey(Gdk.Key key)
        {
            // TODO: Don't use reflection for this! My eyes!!!
            if (key == Gdk.Key.BackSpace)
            {
                return Key.Back;
            }
            else
            {
                string s;
                if (!s_nameDic.TryGetValue(key, out s))
                    s = "Unknown";
                Key result;

                if (Enum.TryParse(s, true, out result))
                {
                    return result;
                }
                else
                {
                    return Key.None;
                }
            }
        }
    }
}