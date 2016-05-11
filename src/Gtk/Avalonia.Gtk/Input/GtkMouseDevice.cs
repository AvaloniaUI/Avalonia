// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Input;
namespace Avalonia.Gtk
{
    public class GtkMouseDevice : MouseDevice
    {
        private static readonly GtkMouseDevice s_instance;

        private Point _clientPosition;

        static GtkMouseDevice()
        {
            s_instance = new GtkMouseDevice();
        }

        private GtkMouseDevice()
        {
        }

        public static new GtkMouseDevice Instance => s_instance;

        internal void SetClientPosition(Point p)
        {
            _clientPosition = p;
        }
    }
}