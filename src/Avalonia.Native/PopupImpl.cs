using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input.TextInput;
using Avalonia.Native.Interop;
using Avalonia.Platform;

namespace Avalonia.Native
{
    class PopupImpl : WindowBaseImpl, IPopupImpl
    {
        private readonly IAvnPopup _native;
        private readonly IWindowBaseImpl _parent;
        private readonly AvaloniaNativeTextInputMethod _inputMethod;

        public PopupImpl(IAvaloniaNativeFactory factory,
            IWindowBaseImpl parent) : base(factory)
        {
            using (var e = new PopupEvents(this))
            {
                Init(_native = factory.CreatePopup(e), factory.CreateScreens());
            }
            
            PopupPositioner = new ManagedPopupPositioner(new ManagedPopupPositionerPopupImplHelper(parent, MoveResize));
            
            _parent = parent;
            
            _native.SetParent((parent as WindowBaseImpl)?.Native);
            
            _inputMethod = new AvaloniaNativeTextInputMethod(_native);
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

        public override IPopupImpl CreatePopup() => new PopupImpl(_factory, this);

        public void SetWindowManagerAddShadowHint(bool enabled)
        {
        }

        public IPopupPositioner PopupPositioner { get; }
        
        public override object TryGetFeature(Type featureType)
        {
            if(featureType == typeof(ITextInputMethodImpl))
            {
                if (_parent != null && _parent.TryGetFeature<ITextInputMethodImpl>(out var inputMethodImpl))
                {
                    return inputMethodImpl;
                }

                return _inputMethod;
            } 
            
            return base.TryGetFeature(featureType);
        }
    }
}
