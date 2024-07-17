using Avalonia.Controls;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Native.Interop;
using Avalonia.Platform;

namespace Avalonia.Native
{
    class PopupImpl : WindowBaseImpl, IPopupImpl
    {
        private readonly ITopLevelImpl _parent;
        private readonly IAvnPopup _native;
        private readonly AvaloniaNativeTextInputMethod _inputMethod;

        public PopupImpl(IAvaloniaNativeFactory factory,
            ITopLevelImpl parent) : base(factory)
        {
            _parent = parent;
            
            using (var e = new PopupEvents(this))
            {
                Init(new MacOSTopLevelHandle(_native = factory.CreatePopup(e)));
            }
            
            PopupPositioner = new ManagedPopupPositioner(new ManagedPopupPositionerPopupImplHelper(parent, MoveResize));

            while (parent is PopupImpl popupImpl)
            {
                parent = popupImpl._parent;
            }

            //Use the parent's input context to process events
            if (parent is TopLevelImpl topLevelImpl)
            {
                _inputMethod = topLevelImpl.InputMethod;
            }
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

        public override void Show(bool activate, bool isDialog)
        {
            var parent = _parent;
            while (parent is PopupImpl p) 
                parent = p._parent;
            if (parent is WindowImpl w)
                w.Native.TakeFocusFromChildren();
            base.Show(false, isDialog);
        }

        public override IPopupImpl CreatePopup() => new PopupImpl(Factory, this);

        public void SetWindowManagerAddShadowHint(bool enabled)
        {
        }

        public IPopupPositioner PopupPositioner { get; }
    }
}
