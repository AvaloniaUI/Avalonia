#nullable enable
using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Input;

namespace Avalonia.X11;
using static XLib;
partial class X11Window
{
    public class XEmbedClientWindowMode : X11WindowMode
    {
        EmbeddableControlRoot? Root => Window._inputRoot as EmbeddableControlRoot;
        private bool _focusedInEmbedder;
        private bool _embedderActivated;
        private bool _disabled;
        private IntPtr _currentEmbedder;
        private bool _suppressConfigureEvents;
        
        public override bool BlockInput => _disabled;
        public double Scaling
        {
            get => Window._scalingOverride ?? 1;
            set => Window._scalingOverride = value;
        }
        
        private WeakReference<IInputElement>? _savedFocus;

        private IInputElement? SavedFocus
        {
            get => _savedFocus?.TryGetTarget(out var target) == true ? target : null;
            set => _savedFocus = value == null ? null : new WeakReference<IInputElement>(value);
        }

        public override void OnHandleCreated(IntPtr handle)
        {
            var data = new[]
            {
                IntPtr.Zero, new(1) /* XEMBED_MAPPED */
            };

            XChangeProperty(Display, handle, X11.Atoms._XEMBED_INFO, X11.Atoms._XEMBED_INFO, 32, PropertyMode.Replace,
                data, data.Length);
            Scaling = 1;
            
            base.OnHandleCreated(handle);
        }

        void SendXEmbedMessage(XEmbedMessage message, IntPtr detail = default, IntPtr data1 = default, IntPtr data2 = default)
        {
            if (_currentEmbedder == IntPtr.Zero)
                return;
            var xev = new XEvent
            {
                ClientMessageEvent =
                {
                    type = XEventName.ClientMessage,
                    send_event = 1,
                    window = _currentEmbedder,
                    message_type = X11.Atoms._XEMBED,
                    format = 32,
                    ptr1 = default,
                    ptr2 = new ((int)message),
                    ptr3 = detail,
                    ptr4 = data1,
                    ptr5 = data2
                }
            };
            XSendEvent(X11.Display, _currentEmbedder, false,
                new IntPtr((int)(EventMask.NoEventMask)), ref xev);
        }
        
        static XEmbedClientWindowMode()
        {
            KeyboardDevice.Instance.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(KeyboardDevice.Instance.FocusedElement))
                {
                    if (KeyboardDevice.Instance.FocusedElement is Visual visual
                        && visual.VisualRoot is EmbeddableControlRoot root
                        && root.PlatformImpl is X11Window window
                        && window._mode is XEmbedClientWindowMode xembedMode
                        && xembedMode._currentEmbedder != IntPtr.Zero)
                    {
                        xembedMode.SavedFocus = KeyboardDevice.Instance.FocusedElement;
                        xembedMode.SendXEmbedMessage(XEmbedMessage.RequestFocus);
                    }
                }
            };
        }

        void Reset()
        {
            _embedderActivated = false;
            _focusedInEmbedder = false;
            _disabled = false;
            UpdateActivation();
        }
        
        void OnXEmbedMessage(IntPtr time, XEmbedMessage message, IntPtr detail, IntPtr data1, IntPtr data2)
        {
            if (message == XEmbedMessage.EmbeddedNotify)
            {
                Reset();
                _currentEmbedder = data1;
            }
            else if (message == XEmbedMessage.FocusIn)
            {
                _focusedInEmbedder = true;
                UpdateActivation();
            }
            else if (message == XEmbedMessage.FocusOut)
            {
                _focusedInEmbedder = false;
                UpdateActivation();
            }
            else if (message == XEmbedMessage.WindowActivate)
            {
                _embedderActivated = true;
                UpdateActivation();
            }
            else if (message == XEmbedMessage.WindowDeactivate)
            {
                _embedderActivated = false;
                UpdateActivation();
            }
            else if (message == XEmbedMessage.ModalityOn)
                _disabled = true;
            else if (message == XEmbedMessage.ModalityOff)
                _disabled = false;
        }

        private void UpdateActivation()
        {
            var active = _focusedInEmbedder && _embedderActivated;

            if (active)
            {
                ((FocusManager?)Root?.FocusManager)?.SetFocusScope(Root);
                SavedFocus?.Focus();
                SavedFocus = null;
            }
            else
            {
                SavedFocus = Root?.IsKeyboardFocusWithin == true ? Root.FocusManager?.GetFocusedElement() : null;
                Window.LostFocus?.Invoke();
            }
        }

        public override bool OnEvent(ref XEvent ev)
        {
            // In this mode we are getting the expected size directly from the embedder
            if (_suppressConfigureEvents && ev.type == XEventName.ConfigureNotify)
                return true;
            if(ev.type == XEventName.MapNotify)
                Root?.StartRendering();
            else if (ev.type == XEventName.UnmapNotify)
                Root?.StopRendering();
            else if (ev.type == XEventName.ReparentNotify)
            {
                Root?.StopRendering();
                _currentEmbedder = IntPtr.Zero;
                Reset();
            }
            else if (ev.type == XEventName.ClientMessage && ev.ClientMessageEvent.message_type == X11.Atoms._XEMBED)
            {
                OnXEmbedMessage(ev.ClientMessageEvent.ptr1,
                    (XEmbedMessage)ev.ClientMessageEvent.ptr2.ToInt32(),
                    ev.ClientMessageEvent.ptr3,
                    ev.ClientMessageEvent.ptr4, ev.ClientMessageEvent.ptr5);
                return true;
            }
            
            return base.OnEvent(ref ev);
        }

        public void ProcessInteractiveResize(PixelSize size)
        {
            _suppressConfigureEvents = true;
            Window._realSize = size;
            Window.Resized?.Invoke(Window.ClientSize, WindowResizeReason.User);
            Window.Paint?.Invoke(new(Window.ClientSize));
        }

        PixelVector GetWindowOffset()
        {
            XTranslateCoordinates(Display, Handle, X11.DefaultRootWindow,
                0, 0, out var offsetX, out var offsetY, out _);
            return new PixelVector(offsetX, offsetY);
        }
        
        public override Point PointToClient(PixelPoint point)
        {
            var pos = GetWindowOffset();
            return new Point(
                (point.X - pos.X) / Window.RenderScaling,
                (point.Y - pos.Y) / Window.RenderScaling);
        }

        public override PixelPoint PointToScreen(Point point) =>
            new PixelPoint(
                (int)(point.X * Window.RenderScaling),
                (int)(point.Y * Window.RenderScaling))
            + GetWindowOffset();
    }
}