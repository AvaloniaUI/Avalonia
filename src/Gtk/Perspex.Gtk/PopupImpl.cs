// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Gtk;
using Perspex.Platform;

namespace Perspex.Gtk
{
    public class PopupImpl : WindowImpl, IPopupImpl
    {
        public PopupImpl()
            : base(WindowType.Popup)
        {
        }
    }
}
