using System.Collections;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Reactive;

namespace Avalonia.Input.GestureRecognizers
{
    public class GestureRecognizerCollection : IReadOnlyCollection<IGestureRecognizer>, IGestureRecognizerActionsDispatcher
    {
        private readonly IInputElement _inputElement;
        private List<IGestureRecognizer>? _recognizers;

        public GestureRecognizerCollection(IInputElement inputElement)
        {
            _inputElement = inputElement;
        }

        public void Add(IGestureRecognizer recognizer)
        {
            if (_recognizers == null)
            {
                // We initialize the collection when the first recognizer is added
                _recognizers = new List<IGestureRecognizer>();
            }

            _recognizers.Add(recognizer);
            recognizer.Initialize(_inputElement, this);

            // Hacks to make bindings work

            if (_inputElement is ILogical logicalParent && recognizer is ISetLogicalParent logical)
            {
                logical.SetParent(logicalParent);
                if (recognizer is StyledElement styleableRecognizer
                    && _inputElement is StyledElement styleableParent)
                    styleableParent.GetObservable(StyledElement.TemplatedParentProperty).Subscribe(parent => styleableRecognizer.TemplatedParent = parent);
            }
        }

        static readonly List<IGestureRecognizer> s_Empty = new List<IGestureRecognizer>();

        public IEnumerator<IGestureRecognizer> GetEnumerator()
            => _recognizers?.GetEnumerator() ?? s_Empty.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => _recognizers?.Count ?? 0;


        internal bool HandlePointerPressed(PointerPressedEventArgs e)
        {
            if (_recognizers == null)
                return false;
            foreach (var r in _recognizers)
            {
                r.PointerPressed(e);
            }

            return e.Handled;
        }

        internal bool HandlePointerReleased(PointerReleasedEventArgs e)
        {
            if (_recognizers == null)
                return false;
            var pointer = e.Pointer as Pointer;

            foreach (var r in _recognizers)
            {
                if (pointer?.CapturedGestureRecognizer != null)
                    break;

                r.PointerReleased(e);
            }
            return e.Handled;
        }

        internal bool HandlePointerMoved(PointerEventArgs e)
        {
            if (_recognizers == null)
                return false;
            var pointer = e.Pointer as Pointer;

            foreach (var r in _recognizers)
            {
                if (pointer?.CapturedGestureRecognizer != null)
                    break;

                r.PointerMoved(e);
            }
            return e.Handled;
        }

        void IGestureRecognizerActionsDispatcher.Capture(IPointer pointer, IGestureRecognizer recognizer)
        {
            var p = pointer as Pointer;

            p?.CaptureGestureRecognizer(recognizer);

            foreach (var r in _recognizers!)
            {
                if (r != recognizer)
                    r.PointerCaptureLost(pointer);
            }
        }

    }
}
