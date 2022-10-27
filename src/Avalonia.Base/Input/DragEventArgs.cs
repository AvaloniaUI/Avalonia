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

        public KeyModifiers KeyModifiers { get; private set; }

        public Point GetPosition(Visual relativeTo)
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

        internal DragEventArgs(RoutedEvent<DragEventArgs> routedEvent, IDataObject data, Interactive target, Point targetLocation, KeyModifiers keyModifiers)
            : base(routedEvent)
        {
            Data = data;
            _target = target;
            _targetLocation = targetLocation;
            KeyModifiers = keyModifiers;
        }
    }
}
