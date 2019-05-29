using System.Collections.Generic;
using System.Linq;
using Avalonia.Input.Raw;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    /// <summary>
    /// Handles raw touch events
    /// <remarks>
    /// This class is supposed to be used on per-toplevel basis, don't use a shared one
    /// </remarks>
    /// </summary>
    public class TouchDevice : IInputDevice
    {
        Dictionary<long, Pointer> _pointers = new Dictionary<long, Pointer>();

        static InputModifiers GetModifiers(InputModifiers modifiers, bool left)
        {
            var mask = (InputModifiers)0x7fffffff ^ InputModifiers.LeftMouseButton ^ InputModifiers.MiddleMouseButton ^
                       InputModifiers.RightMouseButton;
            modifiers &= mask;
            if (left)
                modifiers |= InputModifiers.LeftMouseButton;
            return modifiers;
        }
        
        public void ProcessRawEvent(RawInputEventArgs ev)
        {
            var args = (RawTouchEventArgs)ev;
            if (!_pointers.TryGetValue(args.TouchPointId, out var pointer))
            {
                if (args.Type == RawPointerEventType.TouchEnd)
                    return;
                var hit = args.Root.InputHitTest(args.Position);

                _pointers[args.TouchPointId] = pointer = new Pointer(Pointer.GetNextFreeId(),
                    PointerType.Touch, _pointers.Count == 0, hit);
            }
            

            var target = pointer.GetEffectiveCapture() ?? args.Root;
            if (args.Type == RawPointerEventType.TouchBegin)
            {
                var modifiers = GetModifiers(args.InputModifiers, pointer.IsPrimary);
                target.RaiseEvent(new PointerPressedEventArgs(target, pointer,
                    args.Root, args.Position, new PointerPointProperties(modifiers),
                    modifiers));
            }

            if (args.Type == RawPointerEventType.TouchEnd)
            {
                _pointers.Remove(args.TouchPointId);
                var modifiers = GetModifiers(args.InputModifiers, false);
                using (pointer)
                {
                    target.RaiseEvent(new PointerReleasedEventArgs(target, pointer,
                        args.Root, args.Position, new PointerPointProperties(modifiers),
                        modifiers, pointer.IsPrimary ? MouseButton.Left : MouseButton.None));
                }
            }

            if (args.Type == RawPointerEventType.TouchUpdate)
            {
                var modifiers = GetModifiers(args.InputModifiers, pointer.IsPrimary);
                target.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, target, pointer, args.Root,
                    args.Position, new PointerPointProperties(modifiers), modifiers));
            }
        }
        
    }
}
