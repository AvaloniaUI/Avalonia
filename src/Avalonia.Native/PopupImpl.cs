// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Native.Interop;
using Avalonia.Platform;

namespace Avalonia.Native
{
    public class PopupImpl : WindowBaseImpl, IPopupImpl
    {
        public PopupImpl(IAvaloniaNativeFactory factory, AvaloniaNativePlatformOptions opts) : base(opts)
        {
            using (var e = new PopupEvents(this))
            {
                Init(factory.CreatePopup(e), factory.CreateScreens());
            }
        }

        class PopupEvents : WindowBaseEvents, IAvnWindowEvents
        {
            readonly PopupImpl _parent;

            public PopupEvents(PopupImpl parent) : base(parent)
            {
                _parent = parent;
            }

            bool IAvnWindowEvents.Closing()
            {
                return true;
            }

            void IAvnWindowEvents.WindowStateChanged(AvnWindowState state)
            {
            }
        }
    }
}
