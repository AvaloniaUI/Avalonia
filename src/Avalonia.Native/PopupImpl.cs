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
                Init(new MacOSTopLevelHandle(factory.CreatePopup(e)), factory.CreateScreens());
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

        internal sealed override void Init(MacOSTopLevelHandle handle, IAvnScreens screens)
        {
            base.Init(handle, screens);
        }

        public override void Dispose()
        {
            Native!.SetParent(null);
            
            base.Dispose();
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
        }

        public IPopupPositioner PopupPositioner { get; }
    }
}
