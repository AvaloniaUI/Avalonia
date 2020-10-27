using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.Win32.Interop.Automation;

#nullable enable

namespace Avalonia.Win32.Automation
{
    [ComVisible(true)]
    internal class AutomationProvider : MarshalByRefObject,
        IAutomationPeerImpl,
        IRawElementProviderSimple,
        IRawElementProviderFragment,
        ISelectionProvider
    {
        private readonly UiaControlTypeId _controlType;
        private readonly bool _isContentElement;
        private readonly WeakReference<AutomationPeer> _peer;
        private AutomationProvider? _parent;
        private IRawElementProviderFragmentRoot? _fragmentRoot;
        private Rect _boundingRect;
        private List<AutomationProvider>? _children;
        private bool _childrenValid;
        private string? _className;
        private bool _isKeyboardFocusable;
        private string? _name;
        private SelectionMode _selectionMode;
        private IRawElementProviderSimple[]? _selection;

        public AutomationProvider(
            AutomationPeer peer,
            UiaControlTypeId controlType,
            bool isContentElement)
        {
            Dispatcher.UIThread.VerifyAccess();

            _peer = new WeakReference<AutomationPeer>(peer ?? throw new ArgumentNullException(nameof(peer)));
            _controlType = controlType;
            _isContentElement = isContentElement;
        }

        protected AutomationProvider(AutomationPeer peer)
        {
            Dispatcher.UIThread.VerifyAccess();

            _peer = new WeakReference<AutomationPeer>(peer ?? throw new ArgumentNullException(nameof(peer)));
            _controlType = UiaControlTypeId.Window;
            _isContentElement = true;
        }

        public AutomationPeer Peer
        {
            get
            {
                _peer.TryGetTarget(out var value);
                return value;
            }
        }

        public Rect BoundingRectangle 
        { 
            get
            {
                if (Window is null)
                {
                    throw new NotSupportedException("Non-Window roots not yet supported.");
                }

                return new PixelRect(
                    Window.PointToScreen(_boundingRect.TopLeft),
                    Window.PointToScreen(_boundingRect.BottomRight))
                    .ToRect(1);

            }
        }

        public virtual IRawElementProviderFragmentRoot FragmentRoot
        {
            get
            {
                return _fragmentRoot ??= GetParent()?.FragmentRoot ??
                    throw new AvaloniaInternalException("Could not get FragmentRoot from parent.");
            }
        }
        
        public ProviderOptions ProviderOptions => ProviderOptions.ServerSideProvider;
        public WindowImpl? Window => (FragmentRoot as WindowProvider)?.Owner;
        public virtual IRawElementProviderSimple? HostRawElementProvider => null;
        bool ISelectionProvider.CanSelectMultiple => _selectionMode.HasFlagCustom(SelectionMode.Multiple);
        bool ISelectionProvider.IsSelectionRequired => _selectionMode.HasFlagCustom(SelectionMode.AlwaysSelected);

        [return: MarshalAs(UnmanagedType.IUnknown)]
        public virtual object? GetPatternProvider(int patternId)
        {
            return (UiaPatternId)patternId switch
            {
                UiaPatternId.Selection => Peer is ISelectingAutomationPeer ? this : null,
                _ => null,
            };
        }

        public virtual object? GetPropertyValue(int propertyId)
        {
            return (UiaPropertyId)propertyId switch
            {
                UiaPropertyId.ClassName => _className,
                UiaPropertyId.ControlType => _controlType,
                UiaPropertyId.IsContentElement => _isContentElement,
                UiaPropertyId.IsControlElement => true,
                UiaPropertyId.IsKeyboardFocusable => _isKeyboardFocusable,
                UiaPropertyId.LocalizedControlType => _controlType.ToString().ToLowerInvariant(),
                UiaPropertyId.Name => _name,
                _ => null,
            };
        }

        public int[]? GetRuntimeId() => new int[] { 3, Peer.GetHashCode() };

        public virtual IRawElementProviderFragment? Navigate(NavigateDirection direction)
        {
            if (direction == NavigateDirection.Parent)
            {
                return GetParent();
            }

            EnsureChildren();

            return direction switch
            {
                NavigateDirection.NextSibling => GetParent()?.GetSibling(this, 1),
                NavigateDirection.PreviousSibling => GetParent()?.GetSibling(this, -1),
                NavigateDirection.FirstChild => _children?.FirstOrDefault(),
                NavigateDirection.LastChild => _children?.LastOrDefault(),
                _ => null,
            };
        }

        public void SetFocus()
        {
            InvokeSync(() => Peer.SetFocus());
        }

        public async Task Update()
        {
            if (Dispatcher.UIThread.CheckAccess())
                UpdateCore();
            else
                await Dispatcher.UIThread.InvokeAsync(() => Update());
        }

        public override string ToString() => _className!;

        IRawElementProviderSimple[]? IRawElementProviderFragment.GetEmbeddedFragmentRoots() => null;
        IRawElementProviderSimple[] ISelectionProvider.GetSelection() => _selection ?? Array.Empty<IRawElementProviderSimple>();

        protected void InvokeSync(Action action)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                action();
            }
            else
            {
                Dispatcher.UIThread.InvokeAsync(action).Wait();
            }
        }

        protected T InvokeSync<T>(Func<T> func)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                return func();
            }
            else
            {
                return Dispatcher.UIThread.InvokeAsync(func).Result;
            }
        }

        protected virtual void UpdateCore()
        {
            _boundingRect = Peer.GetBoundingRectangle();
            _className = Peer.GetClassName();
            _isKeyboardFocusable = Peer.IsKeyboardFocusable();
            _name = Peer.GetName();

            if (Peer is ISelectingAutomationPeer selectionPeer)
            {
                var selection = selectionPeer.GetSelection();

                _selectionMode = selectionPeer.GetSelectionMode();
                _selection = selection.Count > 0 ?
                    selection.Select(x => (IRawElementProviderSimple)x.PlatformImpl!).ToArray() :
                    null;
            }
        }

        private AutomationProvider? GetParent()
        {
            if (_parent is null && !(this is IRawElementProviderFragmentRoot))
            {
                _parent = InvokeSync(() => Peer.GetParent())?.PlatformImpl as AutomationProvider ??
                    throw new AvaloniaInternalException($"Could not find parent AutomationProvider for {Peer}.");
            }

            return _parent;
        }

        private void EnsureChildren()
        {
            if (!_childrenValid)
            {
                InvokeSync(() => LoadChildren());
                _childrenValid = true;
            }
        }

        private void LoadChildren()
        {
            var childPeers = InvokeSync(() => Peer.GetChildren());

            _children?.Clear();

            foreach (var childPeer in childPeers)
            {
                _children ??= new List<AutomationProvider>();

                if (childPeer.PlatformImpl is AutomationProvider child)
                {
                    _children.Add(child);
                }
                else
                {
                    throw new AvaloniaInternalException(
                        "AutomationPeer platform implementation not recognised.");
                }
            }
        }

        private IRawElementProviderFragment? GetSibling(AutomationProvider child, int direction)
        {
            EnsureChildren();

            var index = _children?.IndexOf(child) ?? -1;

            if (index >= 0)
            {
                index += direction;

                if (index >= 0 && index < _children!.Count)
                {
                    return _children[index];
                }
            }

            return null;
        }
    }
}
