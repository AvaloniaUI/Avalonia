// -----------------------------------------------------------------------
// <copyright file="GtkKeyboardDevice.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------
namespace Perspex.Gtk
{
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
            get { throw new System.NotImplementedException(); }
        }

        public static Perspex.Input.Key ConvertKey(Gdk.Key key)
        {
            return Key.X;
        }
    }
}

