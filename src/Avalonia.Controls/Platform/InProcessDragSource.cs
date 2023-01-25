using System.Linq;
using Avalonia.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Platform
{
    class InProcessDragSource : IPlatformDragSource
    {
        private const RawInputModifiers MOUSE_INPUTMODIFIERS = RawInputModifiers.LeftMouseButton|RawInputModifiers.MiddleMouseButton|RawInputModifiers.RightMouseButton;
        private readonly IDragDropDevice _dragDrop;
        private readonly IInputManager _inputManager;
        private readonly LightweightSubject<DragDropEffects> _result = new();

        private DragDropEffects _allowedEffects;
        private IDataObject? _draggedData;
        private IInputRoot? _lastRoot;
        private Point _lastPosition;
        private StandardCursorType _lastCursorType;
        private object? _originalCursor;
        private RawInputModifiers? _initialInputModifiers;

        public InProcessDragSource()
        {
            _inputManager = AvaloniaLocator.Current.GetRequiredService<IInputManager>();
            _dragDrop = AvaloniaLocator.Current.GetRequiredService<IDragDropDevice>();
        }

        public async Task<DragDropEffects> DoDragDrop(PointerEventArgs triggerEvent, IDataObject data, DragDropEffects allowedEffects)
        {
            Dispatcher.UIThread.VerifyAccess();
            triggerEvent.Pointer.Capture(null);
            if (_draggedData == null)
            {
                _draggedData = data;
                _lastRoot = null;
                _lastPosition = default;
                _allowedEffects = allowedEffects;

                var inputObserver = new AnonymousObserver<RawInputEventArgs>(arg =>
                {
                    switch (arg)
                    {
                        case RawPointerEventArgs pointerEventArgs:
                            ProcessMouseEvents(pointerEventArgs);
                            break;
                        case RawKeyEventArgs keyEventArgs:
                            ProcessKeyEvents(keyEventArgs);
                            break;
                    }
                }); 
                
                using (_inputManager.PreProcess.Subscribe(inputObserver))
                {
                    var tcs = new TaskCompletionSource<DragDropEffects>();
                    using (_result.Subscribe(new AnonymousObserver<DragDropEffects>(tcs)))
                    {
                        var effect = await tcs.Task;
                        return effect;
                    }
                }
            }
            return DragDropEffects.None;
        }

        private DragDropEffects RaiseEventAndUpdateCursor(RawDragEventType type, IInputRoot root, Point pt, RawInputModifiers modifiers)
        {
            _lastPosition = pt;

            RawDragEvent rawEvent = new RawDragEvent(_dragDrop, type, root, pt, _draggedData!, _allowedEffects, modifiers);
            var tl = (root as Visual)?.GetSelfAndVisualAncestors().OfType<TopLevel>().FirstOrDefault();
            tl?.PlatformImpl?.Input?.Invoke(rawEvent);

            var effect = GetPreferredEffect(rawEvent.Effects & _allowedEffects, modifiers);
            UpdateCursor(root, effect);
            return effect;
        }

        private static DragDropEffects GetPreferredEffect(DragDropEffects effect, RawInputModifiers modifiers)
        {
            if (effect == DragDropEffects.Copy || effect == DragDropEffects.Move || effect == DragDropEffects.Link || effect == DragDropEffects.None)
                return effect; // No need to check for the modifiers.
            if (effect.HasAllFlags(DragDropEffects.Link) && modifiers.HasAllFlags(RawInputModifiers.Alt))
                return DragDropEffects.Link;
            if (effect.HasAllFlags(DragDropEffects.Copy) && modifiers.HasAllFlags(RawInputModifiers.Control))
                return DragDropEffects.Copy;
            return DragDropEffects.Move;
        }

        private static StandardCursorType GetCursorForDropEffect(DragDropEffects effects)
        {
            if (effects.HasAllFlags(DragDropEffects.Copy))
                return StandardCursorType.DragCopy;
            if (effects.HasAllFlags(DragDropEffects.Move))
                return StandardCursorType.DragMove;
            if (effects.HasAllFlags(DragDropEffects.Link))
                return StandardCursorType.DragLink;
            return StandardCursorType.No;
        }
        
        private void UpdateCursor(IInputRoot? root, DragDropEffects effect)
        {
            if (_lastRoot != root)
            {
                if (_lastRoot is InputElement ieLast)
                {
                    if (_originalCursor == AvaloniaProperty.UnsetValue)
                        ieLast.ClearValue(InputElement.CursorProperty);
                    else
                        ieLast.Cursor = _originalCursor as Cursor;
                }

                if (root is InputElement ieNew)
                {
                    if (!ieNew.IsSet(InputElement.CursorProperty))
                        _originalCursor = AvaloniaProperty.UnsetValue;
                    else
                        _originalCursor = root.Cursor;
                }
                else
                    _originalCursor = null;

                _lastCursorType = StandardCursorType.Arrow;
                _lastRoot = root;
            }

            if (root is InputElement ie)
            {
                var ct = GetCursorForDropEffect(effect);
                if (ct != _lastCursorType)
                {
                    _lastCursorType = ct;
                    ie.Cursor = new Cursor(ct);
                }
            }  
        }

        private void CancelDragging()
        {
            if (_lastRoot != null)
                RaiseEventAndUpdateCursor(RawDragEventType.DragLeave, _lastRoot, _lastPosition, RawInputModifiers.None);
            UpdateCursor(null, DragDropEffects.None);
            _result.OnNext(DragDropEffects.None);
        }

        private void ProcessKeyEvents(RawKeyEventArgs e)
        {
            if (e.Type == RawKeyEventType.KeyDown && e.Key == Key.Escape)
            {
                if (_lastRoot != null)
                    RaiseEventAndUpdateCursor(RawDragEventType.DragLeave, _lastRoot, _lastPosition, e.Modifiers);
                UpdateCursor(null, DragDropEffects.None);
                _result.OnNext(DragDropEffects.None);
                e.Handled = true;
            }
            else if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl || e.Key == Key.LeftAlt || e.Key == Key.RightAlt)
            {
                if (_lastRoot != null)
                    RaiseEventAndUpdateCursor(RawDragEventType.DragOver, _lastRoot, _lastPosition, e.Modifiers);
            }
        }

        private void ProcessMouseEvents(RawPointerEventArgs e)
        {
            if (!_initialInputModifiers.HasValue)
                _initialInputModifiers = e.InputModifiers & MOUSE_INPUTMODIFIERS;

            
            void CheckDraggingAccepted(RawInputModifiers changedMouseButton)
            {
                if (_initialInputModifiers.Value.HasAllFlags(changedMouseButton))
                {
                    var result = RaiseEventAndUpdateCursor(RawDragEventType.Drop, e.Root, e.Position, e.InputModifiers);
                    UpdateCursor(null, DragDropEffects.None);
                    _result.OnNext(result);
                }
                else
                    CancelDragging();
                e.Handled = true;
            }
            
            switch (e.Type)
            {
                case RawPointerEventType.LeftButtonDown:
                case RawPointerEventType.RightButtonDown:
                case RawPointerEventType.MiddleButtonDown:
                case RawPointerEventType.NonClientLeftButtonDown:
                    CancelDragging();
                    e.Handled = true;
                    return;
                case RawPointerEventType.LeaveWindow:
                    RaiseEventAndUpdateCursor(RawDragEventType.DragLeave, e.Root, e.Position,  e.InputModifiers); break;
                case RawPointerEventType.LeftButtonUp:
                    CheckDraggingAccepted(RawInputModifiers.LeftMouseButton); break;
                case RawPointerEventType.MiddleButtonUp:
                    CheckDraggingAccepted(RawInputModifiers.MiddleMouseButton); break;
                case RawPointerEventType.RightButtonUp:
                    CheckDraggingAccepted(RawInputModifiers.RightMouseButton); break;
                case RawPointerEventType.Move:
                    var mods = e.InputModifiers & MOUSE_INPUTMODIFIERS;
                    if (_initialInputModifiers.Value != mods)
                    {
                        CancelDragging();
                        e.Handled = true;
                        return;
                    }

                    if (e.Root != _lastRoot)
                    {
                        if (_lastRoot is Visual lr && e.Root is Visual r)
                            RaiseEventAndUpdateCursor(RawDragEventType.DragLeave, _lastRoot, lr.PointToClient(r.PointToScreen(e.Position)), e.InputModifiers);
                        RaiseEventAndUpdateCursor(RawDragEventType.DragEnter, e.Root, e.Position, e.InputModifiers);
                    }
                    else
                        RaiseEventAndUpdateCursor(RawDragEventType.DragOver, e.Root, e.Position, e.InputModifiers);
                    break;
            }
        }
    }
}
