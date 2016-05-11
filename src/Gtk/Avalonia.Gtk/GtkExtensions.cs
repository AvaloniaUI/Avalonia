// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Gtk
{
    using Gtk = global::Gtk;

    public static class GtkExtensions
    {
        public static Rect ToAvalonia(this Gdk.Rectangle rect)
        {
            return new Rect(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }
    }
}
