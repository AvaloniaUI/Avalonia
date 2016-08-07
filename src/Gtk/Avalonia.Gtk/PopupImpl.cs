// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Gtk;
using Avalonia.Platform;

namespace Avalonia.Gtk
{
    public class PopupImpl : WindowImpl, IPopupImpl
    {
        public PopupImpl()
            : base(WindowType.Popup)
        {
        }

		public override Size ClientSize
		{
			get
			{
				int width;
				int height;
				GetSize(out width, out height);
				return new Size(width, height);
			}	
			set
			{
				Resize((int)value.Width, (int)value.Height);
			}
		}
    }
}
