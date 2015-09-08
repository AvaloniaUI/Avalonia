// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Gtk
{
    using Gtk = global::Gtk;

    public static class GtkExtensions
    {
        public static Rect ToPerspex(this Gdk.Rectangle rect)
        {
            return new Rect(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }
    }
}
