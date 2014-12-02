// -----------------------------------------------------------------------
// <copyright file="GtkMouseDevice.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------
namespace Perspex.Gtk
{
    using Perspex.Input;

    public class GtkMouseDevice : MouseDevice
    {
        private static GtkMouseDevice instance;

        static GtkMouseDevice()
        {
            instance = new GtkMouseDevice();
        }

        private GtkMouseDevice()
        {
        }

        public static GtkMouseDevice Instance
        {
            get { return instance; }
        }

        protected override Point GetClientPosition()
        {
            throw new System.NotImplementedException();
        }
    }
}

