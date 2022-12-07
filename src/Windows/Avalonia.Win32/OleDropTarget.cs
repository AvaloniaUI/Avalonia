﻿using System;

using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.MicroCom;
using Avalonia.Platform;
using Avalonia.Win32.Interop;
using MicroCom.Runtime;
using DropEffect = Avalonia.Win32.Win32Com.DropEffect;

namespace Avalonia.Win32
{
    internal class OleDropTarget : CallbackBase, Win32Com.IDropTarget
    {
        private readonly IInputRoot _target;
        private readonly ITopLevelImpl _tl;
        private readonly IDragDropDevice _dragDevice;
        
        private IDataObject _currentDrag = null;

        public OleDropTarget(ITopLevelImpl tl, IInputRoot target)
        {
            _dragDevice = AvaloniaLocator.Current.GetService<IDragDropDevice>();
            _tl = tl;
            _target = target;
        }

        public static DropEffect ConvertDropEffect(DragDropEffects operation)
        {
            DropEffect result = DropEffect.None;
            if (operation.HasAllFlags(DragDropEffects.Copy))
                result |= DropEffect.Copy;
            if (operation.HasAllFlags(DragDropEffects.Move))
                result |= DropEffect.Move;
            if (operation.HasAllFlags(DragDropEffects.Link))
                result |= DropEffect.Link;
            return result;
        }

        public static DragDropEffects ConvertDropEffect(DropEffect effect)
        {
            DragDropEffects result = DragDropEffects.None;
            if (effect.HasAllFlags(DropEffect.Copy))
                result |= DragDropEffects.Copy;
            if (effect.HasAllFlags(DropEffect.Move))
                result |= DragDropEffects.Move;
            if (effect.HasAllFlags(DropEffect.Link))
                result |= DragDropEffects.Link;
            return result;
        }
        
        private static RawInputModifiers ConvertKeyState(int grfKeyState)
        {
            var modifiers = RawInputModifiers.None;
            var state = (UnmanagedMethods.ModifierKeys)grfKeyState;

            if (state.HasAllFlags(UnmanagedMethods.ModifierKeys.MK_LBUTTON))
                modifiers |= RawInputModifiers.LeftMouseButton;
            if (state.HasAllFlags(UnmanagedMethods.ModifierKeys.MK_MBUTTON))
                modifiers |= RawInputModifiers.MiddleMouseButton;
            if (state.HasAllFlags(UnmanagedMethods.ModifierKeys.MK_RBUTTON))
                modifiers |= RawInputModifiers.RightMouseButton;
            if (state.HasAllFlags(UnmanagedMethods.ModifierKeys.MK_SHIFT))
                modifiers |= RawInputModifiers.Shift;
            if (state.HasAllFlags(UnmanagedMethods.ModifierKeys.MK_CONTROL))
                modifiers |= RawInputModifiers.Control;
            if (state.HasAllFlags(UnmanagedMethods.ModifierKeys.MK_ALT))
                modifiers |= RawInputModifiers.Alt;
            return modifiers;
        }

        unsafe void Win32Com.IDropTarget.DragEnter(Win32Com.IDataObject pDataObj, int grfKeyState, UnmanagedMethods.POINT pt, DropEffect* pdwEffect)
        {
            var dispatch = _tl?.Input;
            if (dispatch == null)
            {
                *pdwEffect= (int)DropEffect.None;
            }

            SetDataObject(pDataObj);

            var args = new RawDragEvent(
                _dragDevice,
                RawDragEventType.DragEnter, 
                _target,
                GetDragLocation(pt),
                _currentDrag, 
                ConvertDropEffect(*pdwEffect),
                ConvertKeyState(grfKeyState)
            );
            dispatch(args);
            *pdwEffect = ConvertDropEffect(args.Effects);
        }

        unsafe void Win32Com.IDropTarget.DragOver(int grfKeyState, UnmanagedMethods.POINT pt, DropEffect* pdwEffect)
        {
            var dispatch = _tl?.Input;
            if (dispatch == null)
            {
                *pdwEffect = (int)DropEffect.None;
            }
            
            var args = new RawDragEvent(
                _dragDevice,
                RawDragEventType.DragOver, 
                _target,
                GetDragLocation(pt),
                _currentDrag,
                ConvertDropEffect(*pdwEffect),
                ConvertKeyState(grfKeyState)
            );
            dispatch(args);
            *pdwEffect = ConvertDropEffect(args.Effects);
        }

        void Win32Com.IDropTarget.DragLeave()
        {
            try
            {
                _tl?.Input(new RawDragEvent(
                    _dragDevice,
                    RawDragEventType.DragLeave,
                    _target,
                    default(Point),
                    null,
                    DragDropEffects.None,
                    RawInputModifiers.None
                ));
            }
            finally
            {
                ReleaseDataObject();
            }
        }

        unsafe void Win32Com.IDropTarget.Drop(Win32Com.IDataObject pDataObj, int grfKeyState, UnmanagedMethods.POINT pt, DropEffect* pdwEffect)
        {
            try
            {
                var dispatch = _tl?.Input;
                if (dispatch == null)
                {
                    *pdwEffect = (int)DropEffect.None;
                }

                SetDataObject(pDataObj);

                var args = new RawDragEvent(
                    _dragDevice, 
                    RawDragEventType.Drop, 
                    _target,
                    GetDragLocation(pt),
                    _currentDrag,
                    ConvertDropEffect(*pdwEffect),
                    ConvertKeyState(grfKeyState)
                );
                dispatch(args);
                *pdwEffect = ConvertDropEffect(args.Effects);
            }
            finally
            {
                ReleaseDataObject();
            }
        }

        private void SetDataObject(Win32Com.IDataObject pDataObj)
        {
            var newDrag = GetAvaloniaObjectFromCOM(pDataObj);
            if (_currentDrag != newDrag)
            {
                ReleaseDataObject();
                _currentDrag = newDrag;
            }
        }

        private void ReleaseDataObject()
        {
            // OleDataObject keeps COM reference, so it should be disposed.
            if (_currentDrag is OleDataObject oleDragSource)
            {
                oleDragSource?.Dispose();
                _currentDrag = null;
            }
        }

        private Point GetDragLocation(UnmanagedMethods.POINT dragPoint)
        {
            var screenPt = new PixelPoint(dragPoint.X, dragPoint.Y);
            return ((Visual)_target).PointToClient(screenPt);
        }

        protected override void Destroyed()
        {
            ReleaseDataObject();
        }

        public static unsafe IDataObject GetAvaloniaObjectFromCOM(Win32Com.IDataObject pDataObj)
        {
            if (pDataObj is null)
            {
                throw new ArgumentNullException(nameof(pDataObj));
            }
            if (pDataObj is IDataObject disposableDataObject)
            {
                return disposableDataObject;
            }

            var dataObject = MicroComRuntime.TryUnwrapManagedObject(pDataObj) as DataObject;
            if (dataObject is not null)
            {
                return dataObject;
            }
            return new OleDataObject(pDataObj);
        }
    }
}
