using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
        private const InputModifiers MOUSE_INPUTMODIFIERS = InputModifiers.LeftMouseButton|InputModifiers.MiddleMouseButton|InputModifiers.RightMouseButton;
        private readonly IDragDropDevice _dragDrop;
        private readonly IInputManager _inputManager;
        private readonly Subject<DragDropEffects> _result = new Subject<DragDropEffects>();

        private DragDropEffects _allowedEffects;
        private IDataObject _draggedData;
        private IInputElement _lastRoot;
        private Point _lastPosition;
        private StandardCursorType _lastCursorType;
        private object _originalCursor;
        private InputModifiers? _initialInputModifiers;

        public InProcessDragSource()
        {
            _inputManager = AvaloniaLocator.Current.GetService<IInputManager>();
            _dragDrop = AvaloniaLocator.Current.GetService<IDragDropDevice>();
        }

        public async Task<DragDropEffects> DoDragDrop(IDataObject data, DragDropEffects allowedEffects)
        {
            Dispatcher.UIThread.VerifyAccess();
            if (_draggedData == null)
            {
                _draggedData = data;
                _lastRoot = null;
                _lastPosition = default(Point);
                _allowedEffects = allowedEffects;

                using (_inputManager.PreProcess.OfType<RawPointerEventArgs>().Subscribe(ProcessMouseEvents))
                {
                    using (_inputManager.PreProcess.OfType<RawKeyEventArgs>().Subscribe(ProcessKeyEvents))
                    {
                        var effect = await _result.FirstAsync();
                        return effect;
                    }
                }
            }
            return DragDropEffects.None;
        }


        private DragDropEffects RaiseEventAndUpdateCursor(RawDragEventType type, IInputElement root, Point pt, InputModifiers modifiers)
        {
            _lastPosition = pt;

            RawDragEvent rawEvent = new RawDragEvent(_dragDrop, type, root, pt, _draggedData, _allowedEffects, modifiers);
            var tl = root.GetSelfAndVisualAncestors().OfType<TopLevel>().FirstOrDefault();
            tl.PlatformImpl?.Input(rawEvent);

            var effect = GetPreferredEffect(rawEvent.Effects & _allowedEffects, modifiers);
            UpdateCursor(root, effect);
            return effect;
        }

        private DragDropEffects GetPreferredEffect(DragDropEffects effect, InputModifiers modifiers)
        {
            if (effect == DragDropEffects.Copy || effect == DragDropEffects.Move || effect == DragDropEffects.Link || effect == DragDropEffects.None)
                return effect; // No need to check for the modifiers.
            if (effect.HasFlag(DragDropEffects.Link) && modifiers.HasFlag(InputModifiers.Alt))
                return DragDropEffects.Link;
            if (effect.HasFlag(DragDropEffects.Copy) && modifiers.HasFlag(InputModifiers.Control))
                return DragDropEffects.Copy;
            return DragDropEffects.Move;
        }

        private StandardCursorType GetCursorForDropEffect(DragDropEffects effects)
        {
            if (effects.HasFlag(DragDropEffects.Copy))
                return StandardCursorType.DragCopy;
            if (effects.HasFlag(DragDropEffects.Move))
                return StandardCursorType.DragMove;
            if (effects.HasFlag(DragDropEffects.Link))
                return StandardCursorType.DragLink;
            return StandardCursorType.No;
        }
        
        private void UpdateCursor(IInputElement root, DragDropEffects effect)
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
                RaiseEventAndUpdateCursor(RawDragEventType.DragLeave, _lastRoot, _lastPosition, InputModifiers.None);
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

            
            void CheckDraggingAccepted(InputModifiers changedMouseButton)
            {
                if (_initialInputModifiers.Value.HasFlag(changedMouseButton))
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
                    CheckDraggingAccepted(InputModifiers.LeftMouseButton); break;
                case RawPointerEventType.MiddleButtonUp:
                    CheckDraggingAccepted(InputModifiers.MiddleMouseButton); break;
                case RawPointerEventType.RightButtonUp:
                    CheckDraggingAccepted(InputModifiers.RightMouseButton); break;
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
                        if (_lastRoot != null)
                            RaiseEventAndUpdateCursor(RawDragEventType.DragLeave, _lastRoot, _lastRoot.PointToClient(e.Root.PointToScreen(e.Position)), e.InputModifiers);
                        RaiseEventAndUpdateCursor(RawDragEventType.DragEnter, e.Root, e.Position, e.InputModifiers);
                    }
                    else
                        RaiseEventAndUpdateCursor(RawDragEventType.DragOver, e.Root, e.Position, e.InputModifiers);
                    break;
            }
        }
    }
}
