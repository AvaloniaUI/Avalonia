using System;
using Avalonia.Input.Platform;
using Avalonia.Metadata;

namespace Avalonia.Input.Raw
{
    [PrivateApi]
    public class RawDragEvent : RawInputEventArgs
    {
        [Obsolete] private IDataObject? _legacyDataObject;

        public Point Location { get; set; }

        public IDataTransfer DataTransfer { get; }

        [Obsolete($"Use {nameof(DataTransfer)} instead.")]
        public IDataObject Data
            => _legacyDataObject ??= DataTransfer.ToLegacyDataObject();

        public DragDropEffects Effects { get; set; }

        public RawDragEventType Type { get; }

        public KeyModifiers KeyModifiers { get; }

        [Obsolete($"Use the constructor accepting a {nameof(IDataTransfer)} instance instead.")]
        public RawDragEvent(IDragDropDevice inputDevice,
            RawDragEventType type,
            IInputRoot root,
            Point location,
            IDataObject data,
            DragDropEffects effects,
            RawInputModifiers modifiers)
            : this(inputDevice, type, root, location, new DataObjectToDataTransferWrapper(data), effects, modifiers)
        {
        }

        public RawDragEvent(
            IDragDropDevice inputDevice,
            RawDragEventType type,
            IInputRoot root,
            Point location,
            IDataTransfer dataTransfer,
            DragDropEffects effects,
            RawInputModifiers modifiers)
            : base(inputDevice, 0, root)
        {
            Type = type;
            Location = location;
            DataTransfer = dataTransfer;
            Effects = effects;
            KeyModifiers = modifiers.ToKeyModifiers();
        }
    }
}
