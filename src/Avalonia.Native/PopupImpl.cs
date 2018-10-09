// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Native.Interop;
using Avalonia.Platform;

namespace Avalonia.Native
{
    public class PopupImpl : WindowBaseImpl, IPopupImpl
    {
        IAvnPopup _native;

        public PopupImpl(IAvaloniaNativeFactory factory)
        {
            using (var e = new PopupEvents(this))
            {
                Init(_native = factory.CreatePopup(e), factory.CreateScreens());
            }
        }

        public override void Dispose()
        {
            _native.Dispose();
            base.Dispose();
        }

        class PopupEvents : WindowBaseEvents, IAvnWindowEvents
        {
            readonly PopupImpl _parent;

            public PopupEvents(PopupImpl parent) : base(parent)
            {
                _parent = parent;
            }

            void IAvnWindowEvents.WindowStateChanged(AvnWindowState state)
            {
            }
        }
    }
}
