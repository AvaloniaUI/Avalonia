using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.DragDrop.Raw;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Controls.DragDrop
{
    class DragSource : IPlatformDragSource
    {
        private const InputModifiers MOUSE_INPUTMODIFIERS = InputModifiers.LeftMouseButton|InputModifiers.MiddleMouseButton|InputModifiers.RightMouseButton;
        private readonly IDragDropDevice _dragDrop;
        private readonly IInputManager _inputManager;
        

        private readonly Subject<DragDropEffects> _result = new Subject<DragDropEffects>();
        private IDataObject _draggedData;
        private IInputElement _lastRoot;
        private InputModifiers? _initialInputModifiers;
        private object _lastCursor;

        public DragSource()
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

                using (_inputManager.PreProcess.OfType<RawMouseEventArgs>().Subscribe(e => ProcessMouseEvents(e, allowedEffects)))
                {
                    var effect = await _result.FirstAsync();
                    return effect;
                }
            }
            return DragDropEffects.None;
        }

        private DragDropEffects RaiseDragEvent(RawDragEventType type, IInputElement root, Point pt, DragDropEffects allowedEffects)
        {
            RawDragEvent rawEvent = new RawDragEvent(_dragDrop, type, root, pt, _draggedData, allowedEffects);
            var tl = root.GetSelfAndVisualAncestors().OfType<TopLevel>().FirstOrDefault();
            tl.PlatformImpl.Input(rawEvent);
            
            SetCursor(root, rawEvent.Effects);
            return rawEvent.Effects;
        }

        private Cursor GetCursorForDropEffect(DragDropEffects effects)
        {
            // Todo. Needs to choose cursor by effect.
            if (effects == DragDropEffects.None)
                return new Cursor(StandardCursorType.No);
            return new Cursor(StandardCursorType.Hand);
        }
        
        private void SetCursor(IInputElement root, DragDropEffects effect)
        {
            if (_lastRoot != root)
            {
                if (_lastRoot is InputElement ieLast)
                {
                    if (_lastCursor == AvaloniaProperty.UnsetValue)
                        ieLast.ClearValue(InputElement.CursorProperty);
                    else
                        ieLast.Cursor = _lastCursor as Cursor;
                }

                if (root is InputElement ieNew)
                {
                    if (!ieNew.IsSet(InputElement.CursorProperty))
                        _lastCursor = AvaloniaProperty.UnsetValue;
                    else
                        _lastCursor = root.Cursor;
                }
                else
                    _lastCursor = null;

                _lastRoot = root;
            }

            if (root is InputElement ie)
                ie.Cursor = GetCursorForDropEffect(effect);
        }
        
        private void ProcessMouseEvents(RawMouseEventArgs e, DragDropEffects allowedEffects)
        {
            if (!_initialInputModifiers.HasValue)
                _initialInputModifiers = e.InputModifiers & MOUSE_INPUTMODIFIERS;

            void CancelDragging()
            {
                if (_lastRoot != null)
                    RaiseDragEvent(RawDragEventType.DragLeave, _lastRoot, _lastRoot.PointToClient(e.Root.PointToScreen(e.Position)), allowedEffects);
                SetCursor(null, DragDropEffects.None);
                _result.OnNext(DragDropEffects.None);
                e.Handled = true;
            }
            void AcceptDragging()
            {
                var result = RaiseDragEvent(RawDragEventType.Drop, e.Root, e.Position, allowedEffects) & allowedEffects;
                SetCursor(null, DragDropEffects.None);
                _result.OnNext(result);
                e.Handled = true;
            }
            
            switch (e.Type)
            {
                case RawMouseEventType.LeftButtonDown:
                case RawMouseEventType.RightButtonDown:
                case RawMouseEventType.MiddleButtonDown:
                case RawMouseEventType.NonClientLeftButtonDown:
                    CancelDragging();
                    return;
                case RawMouseEventType.LeaveWindow:
                    RaiseDragEvent(RawDragEventType.DragLeave, e.Root, e.Position, allowedEffects);
                    break;
                case RawMouseEventType.LeftButtonUp:
                    if (_initialInputModifiers.Value.HasFlag(InputModifiers.LeftMouseButton))
                        AcceptDragging();
                    else
                        CancelDragging();
                    return;
                case RawMouseEventType.MiddleButtonUp:
                    if (_initialInputModifiers.Value.HasFlag(InputModifiers.MiddleMouseButton))
                        AcceptDragging();
                    else
                        CancelDragging();
                    return;
                case RawMouseEventType.RightButtonUp:
                    if (_initialInputModifiers.Value.HasFlag(InputModifiers.RightMouseButton))
                        AcceptDragging();
                    else
                        CancelDragging();
                    return;
                case RawMouseEventType.Move:
                    var mods = e.InputModifiers & MOUSE_INPUTMODIFIERS;
                    if (_initialInputModifiers.Value != mods)
                    {
                        CancelDragging();
                        return;
                    }

                    if (e.Root != _lastRoot)
                    {
                        if (_lastRoot != null)
                            RaiseDragEvent(RawDragEventType.DragLeave, _lastRoot, _lastRoot.PointToClient(e.Root.PointToScreen(e.Position)), allowedEffects);
                        RaiseDragEvent(RawDragEventType.DragEnter, e.Root, e.Position, allowedEffects);
                    }
                    else
                        RaiseDragEvent(RawDragEventType.DragOver, e.Root, e.Position, allowedEffects);
                    return;
            }
        }
    }
}
