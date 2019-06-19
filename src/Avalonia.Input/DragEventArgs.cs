using System;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public class DragEventArgs : RoutedEventArgs
    {
        private Interactive _target;
        private Point _targetLocation;

        public DragDropEffects DragEffects { get; set; }

        public IDataObject Data { get; private set; }

        public InputModifiers Modifiers { get; private set; }

        public Point GetPosition(IVisual relativeTo)
        {
            var point = new Point(0, 0);

            if (relativeTo == null)
            {
                throw new ArgumentNullException(nameof(relativeTo));
            }

            if (_target != null)
            {
                point = _target.TranslatePoint(_targetLocation, relativeTo) ?? point;
            }

            return point;
        }

        public DragEventArgs(RoutedEvent<DragEventArgs> routedEvent, IDataObject data, Interactive target, Point targetLocation, InputModifiers modifiers)
            : base(routedEvent)
        {
            this.Data = data;
            this._target = target;
            this._targetLocation = targetLocation;
            this.Modifiers = modifiers;
        }

    }
}
