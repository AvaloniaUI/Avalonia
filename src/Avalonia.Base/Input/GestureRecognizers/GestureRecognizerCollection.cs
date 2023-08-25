using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Reactive;

namespace Avalonia.Input.GestureRecognizers
{
    public class GestureRecognizerCollection : IReadOnlyCollection<GestureRecognizer>
    {
        private readonly IInputElement _inputElement;
        private List<GestureRecognizer>? _recognizers;

        public GestureRecognizerCollection(IInputElement inputElement)
        {
            _inputElement = inputElement;
        }

        public void Add(GestureRecognizer recognizer)
        {
            if (_recognizers == null)
            {
                // We initialize the collection when the first recognizer is added
                _recognizers = new List<GestureRecognizer>();
            }

            _recognizers.Add(recognizer);
            recognizer.Target = _inputElement;

            // Hacks to make bindings work

            if (_inputElement is ILogical logicalParent && recognizer is ISetLogicalParent logical)
            {
                logical.SetParent(logicalParent);
                if (recognizer is StyledElement styleableRecognizer
                    && _inputElement is StyledElement styleableParent)
                    styleableParent.GetObservable(StyledElement.TemplatedParentProperty).Subscribe(parent => styleableRecognizer.TemplatedParent = parent);
            }
        }

        static readonly List<GestureRecognizer> s_Empty = new List<GestureRecognizer>();

        public IEnumerator<GestureRecognizer> GetEnumerator()
            => _recognizers?.GetEnumerator() ?? s_Empty.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => _recognizers?.Count ?? 0;

        internal bool HandlePointerPressed(PointerPressedEventArgs e)
        {
            if (_recognizers == null)
                return false;
            foreach (var r in _recognizers)
            {
                r.PointerPressedInternal(e);
            }

            return e.Handled;
        }

        internal void HandleCaptureLost(IPointer pointer)
        {
            if (_recognizers == null || pointer is not Pointer p)
                return;

            foreach (var r in _recognizers)
            {
                if (p.CapturedGestureRecognizer == r)
                    continue;

                r.PointerCaptureLostInternal(pointer);
            }
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

                r.PointerReleasedInternal(e);
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

                r.PointerMovedInternal(e);
            }
            return e.Handled;
        }
    }
}
