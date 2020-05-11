﻿using System;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Native.Interop;
using Avalonia.Platform;

namespace Avalonia.Native
{
    class PopupImpl : WindowBaseImpl, IPopupImpl
    {
        private readonly IAvaloniaNativeFactory _factory;
        private readonly AvaloniaNativePlatformOptions _opts;
        private readonly GlPlatformFeature _glFeature;

        public PopupImpl(IAvaloniaNativeFactory factory,
            AvaloniaNativePlatformOptions opts,
            GlPlatformFeature glFeature,
            IWindowBaseImpl parent) : base(opts, glFeature)
        {
            _factory = factory;
            _opts = opts;
            _glFeature = glFeature;
            using (var e = new PopupEvents(this))
            {
                var context = _opts.UseGpu ? glFeature?.DeferredContext : null;
                Init(factory.CreatePopup(e, context?.Context), factory.CreateScreens(), context);
            }
            PopupPositioner = new ManagedPopupPositioner(new OsxManagedPopupPositionerPopupImplHelper(parent, MoveResize));
        }

        private void MoveResize(PixelPoint position, Size size, double scaling)
        {
            Position = position;
            Resize(size);
            //TODO: We ignore the scaling override for now
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

        public override IPopupImpl CreatePopup() => new PopupImpl(_factory, _opts, _glFeature, this);
        public IPopupPositioner PopupPositioner { get; }
    }
}
