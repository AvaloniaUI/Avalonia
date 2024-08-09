using Avalonia.Controls;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Native.Interop;
using Avalonia.Platform;

namespace Avalonia.Native
{
    class PopupImpl : WindowBaseImpl, IPopupImpl
    {
        private readonly ITopLevelImpl _parent;

        public PopupImpl(IAvaloniaNativeFactory factory,
            ITopLevelImpl parent) : base(factory)
        {
            _parent = parent;
            
            using (var e = new PopupEvents(this))
            {
                Init(new MacOSTopLevelHandle(factory.CreatePopup(e)));
            }
            
            PopupPositioner = new ManagedPopupPositioner(new ManagedPopupPositionerPopupImplHelper(parent, MoveResize));

            while (parent is PopupImpl popupImpl)
            {
                parent = popupImpl._parent;
            }

            if (parent is WindowBaseImpl windowBaseImpl)
            {
                Native!.SetParent(windowBaseImpl.Native);
            }
            
            //Use the parent's input context to process events
            if (parent is TopLevelImpl topLevelImpl)
            {
                InputMethod = topLevelImpl.InputMethod;
            }
        }

        public override void Dispose()
        {
            Native!.SetParent(null);
            
            base.Dispose();
        }

        internal sealed override void Init(MacOSTopLevelHandle handle)
        {
            base.Init(handle);
        }

        private void MoveResize(PixelPoint position, Size size, double scaling)
        {
            Position = position;
            Resize(size, WindowResizeReason.Layout);
            //TODO: We ignore the scaling override for now
        }

        class PopupEvents : WindowBaseEvents, IAvnWindowEvents
        {
            readonly PopupImpl _parent;

            public PopupEvents(PopupImpl parent) : base(parent)
            {
                _parent = parent;
            }

            public void GotInputWhenDisabled()
            {
                // NOP on Popup
            }

            int IAvnWindowEvents.Closing()
            {
                return true.AsComBool();
            }

            void IAvnWindowEvents.WindowStateChanged(AvnWindowState state)
            {
            }
        }

        public override IPopupImpl CreatePopup() => new PopupImpl(Factory, this);

        public void SetWindowManagerAddShadowHint(bool enabled)
        {
        }

        public void TakeFocus()
        {
            var parent = _parent;

            while (parent != null)
            {
                if (parent is PopupImpl popup)
                    parent = popup._parent;
                else
                    break;
            }
            
            if (parent is WindowImpl w)
                w.Native.TakeFocusFromChildren();
        }

        public IPopupPositioner PopupPositioner { get; }
    }
}
