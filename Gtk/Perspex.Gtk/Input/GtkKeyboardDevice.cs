// -----------------------------------------------------------------------
// <copyright file="GtkKeyboardDevice.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Gtk
{
    using System;
    using Perspex.Input;

    public class GtkKeyboardDevice : KeyboardDevice
    {
        private static GtkKeyboardDevice instance;

        static GtkKeyboardDevice()
        {
            instance = new GtkKeyboardDevice();
        }

        private GtkKeyboardDevice()
        {
        }

        public static GtkKeyboardDevice Instance
        {
            get { return instance; }
        }

        public override ModifierKeys Modifiers
        {
            get
            {
                // TODO: Implement.
                return ModifierKeys.None;
            }
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
                var s = Enum.GetName(typeof(Gdk.Key), key);
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