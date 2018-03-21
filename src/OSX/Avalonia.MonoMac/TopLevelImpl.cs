using System;
using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Rendering;
using Avalonia.Threading;
using MonoMac.AppKit;
using MonoMac.CoreFoundation;
using MonoMac.CoreGraphics;
using MonoMac.Foundation;
using MonoMac.ObjCRuntime;

namespace Avalonia.MonoMac
{
    abstract class TopLevelImpl : ITopLevelImpl, IFramebufferPlatformSurface
    {
        public TopLevelView View { get; }
        private readonly IMouseDevice _mouse = AvaloniaLocator.Current.GetService<IMouseDevice>();
        protected TopLevelImpl()
        {
            View = new TopLevelView(this);
        }

        protected virtual void OnInput(RawInputEventArgs args)
        {
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);
            Input?.Invoke(args);
        }

        [Adopts("NSTextInputClient")]
        public class TopLevelView : NSView
        {
            TopLevelImpl _tl;
            bool _isLeftPressed, _isRightPressed, _isMiddlePressed;
            private readonly IMouseDevice _mouse;
            private readonly IKeyboardDevice _keyboard;
            private NSTrackingArea _area;
            private NSCursor _cursor;
            private bool _nonUiRedrawQueued;
            private bool _isMouseOver;

            public CGSize PixelSize { get; set; }

            public CGSize LogicalSize { get; set; }

            private SavedImage _backBuffer;
            public object SyncRoot { get; } = new object();

            public TopLevelView(TopLevelImpl tl)
            {
                _tl = tl;
                _mouse = AvaloniaLocator.Current.GetService<IMouseDevice>();
                _keyboard = AvaloniaLocator.Current.GetService<IKeyboardDevice>();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _backBuffer?.Dispose();
                    _backBuffer = null;
                }
                base.Dispose(disposing);
            }

            public override bool ConformsToProtocol(IntPtr protocol)
            {
                var rv = base.ConformsToProtocol(protocol);
                return rv;
            }

            public override bool IsOpaque => false;

            public override void DrawRect(CGRect dirtyRect)
            {
                lock (SyncRoot)
                    _nonUiRedrawQueued = false;
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Render);
                lock (SyncRoot)
                {
                    if (_backBuffer != null)
                    {
                        using (var context = NSGraphicsContext.CurrentContext.GraphicsPort)
                        {
                            context.SetFillColor(255, 255, 255, 255);
                            context.FillRect(new CGRect(default(CGPoint), LogicalSize));
                            context.TranslateCTM(0, LogicalSize.Height - _backBuffer.LogicalSize.Height);
                            context.DrawImage(new CGRect(default(CGPoint), _backBuffer.LogicalSize), _backBuffer.Image);
                            context.Flush();
                            NSGraphicsContext.CurrentContext.FlushGraphics();
                        }
                    }
                }
                _tl.Paint?.Invoke(dirtyRect.ToAvaloniaRect());
            }

            public void SetBackBufferImage(SavedImage image)
            {
                lock (SyncRoot)
                {
                    _backBuffer?.Dispose();
                    _backBuffer = image;
                    if (image == null)
                        return;

                    if (_nonUiRedrawQueued)
                        return;
                    _nonUiRedrawQueued = true;
                    Dispatcher.UIThread.Post(
                        () =>
                        {
                            lock (SyncRoot)
                            {
                                if (!_nonUiRedrawQueued)
                                    return;
                                _nonUiRedrawQueued = false;
                            }
                            SetNeedsDisplayInRect(Frame);
                            Display();
                        }, DispatcherPriority.Render);

                }
            }
            
            [Export("viewDidChangeBackingProperties:")]
            public void ViewDidChangeBackingProperties()
            {
                _tl?.ScalingChanged?.Invoke(_tl.Scaling);
            }

            void UpdateCursor()
            {
                ResetCursorRects();
                if (_cursor != null)
                {
                    AddCursorRect(Frame, _cursor);
                    if (_isMouseOver)
                        _cursor.Set();
                }
            }

            static readonly NSCursor ArrowCursor = NSCursor.ArrowCursor;

            public void SetCursor(NSCursor cursor)
            {
                _cursor = cursor ?? ArrowCursor;
                UpdateCursor();
            }

            public override void SetFrameSize(CGSize newSize)
            {
                lock (SyncRoot)
                {
                    base.SetFrameSize(newSize);
                    LogicalSize = Frame.Size;
                    PixelSize = ConvertSizeToBacking(LogicalSize);
                }

                if (_area != null)
                {
                    RemoveTrackingArea(_area);
                    _area.Dispose();
                }
                _area = new NSTrackingArea(new CGRect(default(CGPoint), newSize),
                    NSTrackingAreaOptions.ActiveAlways |
                    NSTrackingAreaOptions.MouseMoved |
                    NSTrackingAreaOptions.EnabledDuringMouseDrag, this, null);
                AddTrackingArea(_area);
                UpdateCursor();
                _tl?.Resized?.Invoke(_tl.ClientSize);
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Layout);
            }

            InputModifiers GetModifiers(NSEventModifierMask mod)
            {
                var rv = new InputModifiers();
                if (mod.HasFlag(NSEventModifierMask.ControlKeyMask))
                    rv |= InputModifiers.Control;
                if (mod.HasFlag(NSEventModifierMask.ShiftKeyMask))
                    rv |= InputModifiers.Shift;
                if (mod.HasFlag(NSEventModifierMask.AlternateKeyMask))
                    rv |= InputModifiers.Alt;
                if (mod.HasFlag(NSEventModifierMask.CommandKeyMask))
                    rv |= InputModifiers.Windows;

                if (_isLeftPressed)
                    rv |= InputModifiers.LeftMouseButton;
                if (_isMiddlePressed)
                    rv |= InputModifiers.MiddleMouseButton;
                if (_isRightPressed)
                    rv |= InputModifiers.RightMouseButton;
                return rv;
            }

            public Point TranslateLocalPoint(Point pt) => pt.WithY(Bounds.Height - pt.Y);

            Vector GetDelta(NSEvent ev)
            {
                var rv = new Vector(ev.ScrollingDeltaX, ev.ScrollingDeltaY);
                //TODO: Verify if handling of HasPreciseScrollingDeltas
                // is required (touchpad or magic-mouse is needed)
                return rv;
            }

            uint GetTimeStamp(NSEvent ev) => (uint) (ev.Timestamp * 1000);

            void MouseEvent(NSEvent ev, RawMouseEventType type)
            {
                BecomeFirstResponder();
                var loc = TranslateLocalPoint(ConvertPointToView(ev.LocationInWindow, this).ToAvaloniaPoint());
                var ts = GetTimeStamp(ev);
                var mod = GetModifiers(ev.ModifierFlags);
                if (type == RawMouseEventType.Wheel)
                {
                    var delta = GetDelta(ev);
                    // ReSharper disable CompareOfFloatsByEqualityOperator
                    if (delta.X == 0 && delta.Y == 0)
                        return;
                    // ReSharper restore CompareOfFloatsByEqualityOperator
                    _tl.OnInput(new RawMouseWheelEventArgs(_mouse, ts, _tl.InputRoot, loc,
                        delta, mod));
                }
                else
                    _tl.OnInput(new RawMouseEventArgs(_mouse, ts, _tl.InputRoot, type, loc, mod));
            }

            public override void MouseMoved(NSEvent theEvent)
            {
                MouseEvent(theEvent, RawMouseEventType.Move);
                base.MouseMoved(theEvent);
            }

            public override void MouseDragged(NSEvent theEvent)
            {
                MouseEvent(theEvent, RawMouseEventType.Move);
                base.MouseDragged(theEvent);
            }

            public override void OtherMouseDragged(NSEvent theEvent)
            {
                MouseEvent(theEvent, RawMouseEventType.Move);
                base.OtherMouseDragged(theEvent);
            }

            public override void RightMouseDragged(NSEvent theEvent)
            {
                MouseEvent(theEvent, RawMouseEventType.Move);
                base.RightMouseDragged(theEvent);
            }

            public NSEvent LastMouseDownEvent { get; private set; }

            public override void MouseDown(NSEvent theEvent)
            {
                _isLeftPressed = true;
                LastMouseDownEvent = theEvent;
                MouseEvent(theEvent, RawMouseEventType.LeftButtonDown);
                LastMouseDownEvent = null;
                base.MouseDown(theEvent);
            }

            public override void RightMouseDown(NSEvent theEvent)
            {
                _isRightPressed = true;
                MouseEvent(theEvent, RawMouseEventType.RightButtonDown);
                base.RightMouseDown(theEvent);
            }

            public override void OtherMouseDown(NSEvent theEvent)
            {
                _isMiddlePressed = true;
                MouseEvent(theEvent, RawMouseEventType.MiddleButtonDown);
                base.OtherMouseDown(theEvent);
            }

            public override void MouseUp(NSEvent theEvent)
            {
                _isLeftPressed = false;
                MouseEvent(theEvent, RawMouseEventType.LeftButtonUp);
                base.MouseUp(theEvent);
            }

            public override void RightMouseUp(NSEvent theEvent)
            {
                _isRightPressed = false;
                MouseEvent(theEvent, RawMouseEventType.RightButtonUp);
                base.RightMouseUp(theEvent);
            }

            public override void OtherMouseUp(NSEvent theEvent)
            {
                _isMiddlePressed = false;
                MouseEvent(theEvent, RawMouseEventType.MiddleButtonUp);
                base.OtherMouseUp(theEvent);
            }

            public override void ScrollWheel(NSEvent theEvent)
            {
                MouseEvent(theEvent, RawMouseEventType.Wheel);
                base.ScrollWheel(theEvent);
            }

            public override void MouseExited(NSEvent theEvent)
            {
                _isMouseOver = false;
                MouseEvent(theEvent, RawMouseEventType.LeaveWindow);
                base.MouseExited(theEvent);
            }

            public override void MouseEntered(NSEvent theEvent)
            {
                _isMouseOver = true;
                base.MouseEntered(theEvent);
            }

            void KeyboardEvent(RawKeyEventType type, NSEvent ev)
            {
                var code = KeyTransform.TransformKeyCode(ev.KeyCode);
                if (!code.HasValue)
                    return;
                _tl.OnInput(new RawKeyEventArgs(_keyboard, GetTimeStamp(ev),
                    type, code.Value, GetModifiers(ev.ModifierFlags)));
            }

            public override void KeyDown(NSEvent theEvent)
            {
                KeyboardEvent(RawKeyEventType.KeyDown, theEvent);
                InputContext.HandleEvent(theEvent);
                base.KeyDown(theEvent);
            }

            public override void KeyUp(NSEvent theEvent)
            {
                KeyboardEvent(RawKeyEventType.KeyUp, theEvent);
                base.KeyUp(theEvent);
            }



            #region NSTextInputClient

            public override bool AcceptsFirstResponder() => true;

            public bool HasMarkedText
            {
                [Export("hasMarkedText")] get => false;
            }

            public NSRange MarkedRange
            {
                [Export("markedRange")] get => new NSRange(NSRange.NotFound, 0);
            }

            public NSRange SelectedRange
            {
                [Export("selectedRange")] get => new NSRange(NSRange.NotFound, 0);
            }

            [Export("setMarkedText:selectedRange:replacementRange:")]
            public void SetMarkedText(NSString str, NSRange a1, NSRange a2)
            {

            }

            [Export("unmarkText")]
            public void UnmarkText()
            {

            }

            public NSArray ValidAttributesForMarkedText
            {
                [Export("validAttributesForMarkedText")] get => new NSArray();
            }

            [Export("attributedSubstringForProposedRange:actualRange:")]
            public NSAttributedString AttributedSubstringForProposedRange(NSRange range, IntPtr wat)
            {
                return new NSAttributedString("");
            }

            [Export("insertText:replacementRange:")]
            public void InsertText(NSString str, NSRange range)
            {
                //TODO: timestamp
                _tl.OnInput(new RawTextInputEventArgs(_keyboard, 0, str.ToString()));
            }

            [Export("characterIndexForPoint:")]
            public uint CharacterIndexForPoint(CGPoint pt)
            {
                return 0;
            }

            [Export("firstRectForCharacterRange:actualRange:")]
            public CGRect FirstRectForCharacterRange(NSRange range, IntPtr wat)
            {
                return new CGRect();
            }

            #endregion
        }

        public IInputRoot InputRoot { get; private set; }

        public abstract Size ClientSize { get; }

        public double Scaling
        {
            get
            {
                if (View.Window == null)
                    return 1;
                return View.Window.BackingScaleFactor;
            }
        }

        public IEnumerable<object> Surfaces => new[] {this};
        public IMouseDevice MouseDevice => _mouse;
        
        public Action<RawInputEventArgs> Input { get; set; }
        public Action<Rect> Paint { get; set; }
        public Action<Size> Resized { get; set; }
        public Action<double> ScalingChanged { get; set; }
        public Action Closed { get; set; }

        
        public virtual void Dispose()
        {
            Closed?.Invoke();
            Closed = null;
            View.Dispose();
        }

        public IRenderer CreateRenderer(IRenderRoot root) =>
            MonoMacPlatform.UseDeferredRendering
                ? new DeferredRenderer(root, AvaloniaLocator.Current.GetService<IRenderLoop>())
                : (IRenderer) new ImmediateRenderer(root);

        public void Invalidate(Rect rect)
        {
            if (!MonoMacPlatform.UseDeferredRendering)
                View.SetNeedsDisplayInRect(View.Frame);
        }

        public abstract Point PointToClient(Point point);

        public abstract Point PointToScreen(Point point);

        public void SetCursor(IPlatformHandle cursor) => View.SetCursor((cursor as Cursor)?.Native);

        public void SetInputRoot(IInputRoot inputRoot) => InputRoot = inputRoot;

        public ILockedFramebuffer Lock() => new EmulatedFramebuffer(View);
    }
}