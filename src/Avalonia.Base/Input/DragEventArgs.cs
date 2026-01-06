using System;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Metadata;

namespace Avalonia.Input
{
    public class DragEventArgs : RoutedEventArgs, IKeyModifiersEventArgs
    {
        private readonly Interactive _target;
        private readonly Point _targetLocation;
        [Obsolete] private IDataObject? _legacyDataObject;

        public DragDropEffects DragEffects { get; set; }

        public IDataTransfer DataTransfer { get; }

        [Obsolete($"Use {nameof(DataTransfer)} instead.")]
        public IDataObject Data
            => _legacyDataObject ??= DataTransfer.ToLegacyDataObject();

        public KeyModifiers KeyModifiers { get; }

        public Point GetPosition(Visual relativeTo)
        {
            if (relativeTo == null)
            {
                throw new ArgumentNullException(nameof(relativeTo));
            }

            return _target.TranslatePoint(_targetLocation, relativeTo) ?? new Point(0, 0);
        }

        [Obsolete($"Use the constructor accepting a {nameof(IDataTransfer)} instance instead.")]
        public DragEventArgs(
            RoutedEvent<DragEventArgs> routedEvent,
            IDataObject data,
            Interactive target,
            Point targetLocation,
            KeyModifiers keyModifiers)
            : this(routedEvent, new DataObjectToDataTransferWrapper(data), target, targetLocation, keyModifiers)
        {
        }

        [Unstable("This constructor might be removed in 12.0. For unit testing, consider using DragDrop.DoDragDrop or IHeadlessWindow.DragDrop.")]
        public DragEventArgs(
            RoutedEvent<DragEventArgs> routedEvent,
            IDataTransfer dataTransfer,
            Interactive target,
            Point targetLocation,
            KeyModifiers keyModifiers)
            : base(routedEvent)
        {
            DataTransfer = dataTransfer;
            _target = target;
            _targetLocation = targetLocation;
            KeyModifiers = keyModifiers;
        }
    }
}
