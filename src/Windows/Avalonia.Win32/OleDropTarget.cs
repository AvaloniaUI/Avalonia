using Avalonia.Controls.DragDrop;
using Avalonia.Input;
using Avalonia.Win32.Interop;
using IDataObject = Avalonia.Controls.DragDrop.IDataObject;
using IOleDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace Avalonia.Win32
{
    class OleDropTarget : IDropTarget
    {
        private readonly IDragDispatcher _dragDispatcher;
        private readonly IInputElement _target;
        
        private IDataObject _currentDrag = null;

        public OleDropTarget(IInputElement target)
        {
            _dragDispatcher = AvaloniaLocator.Current.GetService<IDragDispatcher>();
            _target = target;
        }

        static DropEffect ConvertDropEffect(DragDropEffects operation)
        {
            DropEffect result = DropEffect.None;
            if (operation.HasFlag(DragDropEffects.Copy))
                result |= DropEffect.Copy;
            if (operation.HasFlag(DragDropEffects.Move))
                result |= DropEffect.Move;
            if (operation.HasFlag(DragDropEffects.Link))
                result |= DropEffect.Link;
            return result;
        }

        static DragDropEffects ConvertDropEffect(DropEffect effect)
        {
            DragDropEffects result = DragDropEffects.None;
            if (effect.HasFlag(DropEffect.Copy))
                result |= DragDropEffects.Copy;
            if (effect.HasFlag(DropEffect.Move))
                result |= DragDropEffects.Move;
            if (effect.HasFlag(DropEffect.Link))
                result |= DragDropEffects.Link;
            return result;
        }

        UnmanagedMethods.HRESULT IDropTarget.DragEnter(IOleDataObject pDataObj, int grfKeyState, long pt, ref DropEffect pdwEffect)
        {
            if (_dragDispatcher == null)
            {
                pdwEffect = DropEffect.None;
                return UnmanagedMethods.HRESULT.S_OK;
            }
            
            _currentDrag = new OleDataObject(pDataObj);
            var dragLocation = GetDragLocation(pt);

            var operation = ConvertDropEffect(pdwEffect);
            operation = _dragDispatcher.DragEnter(_target, dragLocation, _currentDrag, operation);
            pdwEffect = ConvertDropEffect(operation);
            
            return UnmanagedMethods.HRESULT.S_OK;
        }

        UnmanagedMethods.HRESULT IDropTarget.DragOver(int grfKeyState, long pt, ref DropEffect pdwEffect)
        {
            if (_dragDispatcher == null)
            {
                pdwEffect = DropEffect.None;
                return UnmanagedMethods.HRESULT.S_OK;
            }
            
            var dragLocation = GetDragLocation(pt);

            var operation = ConvertDropEffect(pdwEffect);
            operation = _dragDispatcher.DragOver(_target, dragLocation, _currentDrag, operation);
            pdwEffect = ConvertDropEffect(operation);
            
            return UnmanagedMethods.HRESULT.S_OK;  
        }

        UnmanagedMethods.HRESULT IDropTarget.DragLeave()
        {
            try
            {
                _dragDispatcher?.DragLeave(_target);
                return UnmanagedMethods.HRESULT.S_OK;
            }
            finally
            {
                _currentDrag = null;
            }
        }

        UnmanagedMethods.HRESULT IDropTarget.Drop(IOleDataObject pDataObj, int grfKeyState, long pt, ref DropEffect pdwEffect)
        {
            try
            {
                if (_dragDispatcher == null)
                {
                    pdwEffect = DropEffect.None;
                    return UnmanagedMethods.HRESULT.S_OK;
                }
            
                _currentDrag= new OleDataObject(pDataObj);
                var dragLocation = GetDragLocation(pt);

                var operation = ConvertDropEffect(pdwEffect);
                operation = _dragDispatcher.Drop(_target, dragLocation, _currentDrag, operation);
                pdwEffect = ConvertDropEffect(operation);
            
                return UnmanagedMethods.HRESULT.S_OK;  
            }
            finally
            {
                _currentDrag = null;
            }
        }

        private Point GetDragLocation(long dragPoint)
        {
            int x = (int)dragPoint;
            int y = (int)(dragPoint >> 32);

            Point screenPt = new Point(x, y);
            return _target.PointToClient(screenPt);
        }
    }
}