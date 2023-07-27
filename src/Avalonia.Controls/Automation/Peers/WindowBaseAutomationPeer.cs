using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Automation.Peers
{
    public class WindowBaseAutomationPeer : ControlAutomationPeer, IRootProvider
    {
        private Control? _focus;

        public WindowBaseAutomationPeer(WindowBase owner)
            : base(owner)
        {
        }

        public new WindowBase Owner => (WindowBase)base.Owner;
        public ITopLevelImpl? PlatformImpl => Owner.PlatformImpl;

        public event EventHandler? FocusChanged;

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Window;
        }

        public AutomationPeer? GetFocus() => _focus is object ? GetOrCreate(_focus) : null;

        public AutomationPeer? GetPeerFromPoint(Point p)
        {
            var hit = Owner.GetVisualAt(p)?.FindAncestorOfType<Control>(includeSelf: true);

            if (hit is null)
                return null;

            var peer = GetOrCreate(hit);

            while (peer != this && peer.GetProvider<IEmbeddedRootProvider>() is { } embedded)
            {
                var embeddedHit = embedded.GetPeerFromPoint(p);
                if (embeddedHit is null)
                    break;
                peer = embeddedHit;
            }

            return peer;
        }

        protected void StartTrackingFocus()
        {
            if (KeyboardDevice.Instance is not null)
            {
                KeyboardDevice.Instance.PropertyChanged += KeyboardDevicePropertyChanged;
                OnFocusChanged(KeyboardDevice.Instance.FocusedElement);
            }
        }

        protected void StopTrackingFocus()
        {
            if (KeyboardDevice.Instance is not null)
                KeyboardDevice.Instance.PropertyChanged -= KeyboardDevicePropertyChanged;
        }

        private void OnFocusChanged(IInputElement? focus)
        {
            var oldFocus = _focus;
            var c = focus as Control;
            
            _focus = c?.VisualRoot == Owner ? c : null;
            
            if (_focus != oldFocus)
            {
                var peer = _focus is object ?
                    _focus == Owner ? this :
                    GetOrCreate(_focus) : null;
                FocusChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void KeyboardDevicePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(KeyboardDevice.FocusedElement))
            {
                OnFocusChanged(KeyboardDevice.Instance!.FocusedElement);
            }
        }
    }
}


