using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System.Linq;

namespace Avalonia.Controls.DragDrop
{
    class DefaultDragDispatcher : IDragDispatcher
    {
        public static readonly DefaultDragDispatcher Instance = new DefaultDragDispatcher();

        private Interactive _lastTarget = null;
        
        private DefaultDragDispatcher()
        {   
        }

        private Interactive GetTarget(IInputElement root, Point local)
        {
            var target = root.InputHitTest(local)?.GetSelfAndVisualAncestors()?.OfType<Interactive>()?.FirstOrDefault();
            if (target != null && DragDrop.GetAcceptDrag(target))
                return target;
            return null;
        }
        
        private DragOperation RaiseDragEvent(Interactive target, RoutedEvent<DragEventArgs> routedEvent, DragOperation operation, IDragData data)
        {
            if (target == null)
                return DragOperation.None;
            var args = new DragEventArgs(routedEvent, data)
            {
                RoutedEvent = routedEvent,
                DragOperation = operation
            };
            target.RaiseEvent(args);
            return args.DragOperation;
        }
        
        public DragOperation DragEnter(IInputElement inputRoot, Point point, IDragData data, DragOperation operation)
        {
            _lastTarget = GetTarget(inputRoot, point);
            return RaiseDragEvent(_lastTarget, DragDrop.DragEnterEvent, operation, data);
        }

        public DragOperation DragOver(IInputElement inputRoot, Point point, IDragData data, DragOperation operation)
        {
            var target = GetTarget(inputRoot, point);

            if (target == _lastTarget)
                return RaiseDragEvent(target, DragDrop.DragOverEvent, operation, data);
            
            try
            {
                if (_lastTarget != null)
                    _lastTarget.RaiseEvent(new RoutedEventArgs(DragDrop.DragLeaveEvent));
                return RaiseDragEvent(target, DragDrop.DragEnterEvent, operation, data);
            }
            finally
            {
                _lastTarget = target;
            }            
        }

        public void DragLeave(IInputElement inputRoot)
        {
            if (_lastTarget == null)
                return;
            try
            {
                _lastTarget.RaiseEvent(new RoutedEventArgs(DragDrop.DragLeaveEvent));
            }
            finally 
            {
                _lastTarget = null;
            }
        }

        public DragOperation Drop(IInputElement inputRoot, Point point, IDragData data, DragOperation operation)
        {
            try
            {
                return RaiseDragEvent(_lastTarget, DragDrop.DropEvent, operation, data);
            }
            finally 
            {
                _lastTarget = null;
            }
        }
    }
}