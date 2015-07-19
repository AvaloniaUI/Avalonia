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

        private Point clientPosition;

        static GtkMouseDevice()
        {
            instance = new GtkMouseDevice();
        }

        private GtkMouseDevice()
        {
        }

        public static new GtkMouseDevice Instance
        {
            get { return instance; }
        }

        internal void SetClientPosition(Point p)
        {
            this.clientPosition = p;
        }
    }
}