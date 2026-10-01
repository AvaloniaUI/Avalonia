using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Base.UnitTests.Input
{
    public class PointerTests : PointerTestsBase
    {
        [Fact]
        public void On_Capture_Transfer_PointerCaptureLost_Should_Propagate_Up_To_The_Common_Parent()
        {
            Border initialParent, initialCapture, newParent, newCapture;
            var el = new StackPanel
            {
                Children =
                {
                    (initialParent = new Border { Child = initialCapture = new Border() }),
                    (newParent = new Border { Child = newCapture = new Border() })
                }
            };
            var receivers = new List<object?>();
            var root = new TestRoot(el);
            foreach (InputElement d in root.GetSelfAndVisualDescendants())
                d.PointerCaptureLost += (s, e) => receivers.Add(s);
            var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            
            pointer.Capture(initialCapture);
            pointer.Capture(newCapture);
            Assert.True(receivers.SequenceEqual(new[] { initialCapture, initialParent }));
            
            receivers.Clear();
            pointer.Capture(null);
            Assert.True(receivers.SequenceEqual(new object[] { newCapture, newParent, el, root }));
        }

        [Fact]
        public void Capture_Captured_ShouldNot_Call_Platform()
        {
            var pointer = new TestPointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);

            Border capture = new Border();
            pointer.Capture(capture);
            pointer.Capture(capture);

            Assert.Equal(1, pointer.PlatformCaptureCalled);

            pointer.Capture(null);
            pointer.Capture(null);

            Assert.Equal(2, pointer.PlatformCaptureCalled);
        }

        [Fact]
        public void Gesture_Recognizer_Capture_Should_Keep_Platform_Capture_On_Same_Target()
        {
            var pointer = new TestPointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            var target = new Border();
            var recognizer = new TestGestureRecognizer { Target = target };

            pointer.Capture(target, CaptureSource.Implicit);
            recognizer.CapturePointer(pointer);

            Assert.Null(pointer.Captured);
            Assert.Same(recognizer, pointer.CapturedGestureRecognizer);
            Assert.Equal([target], pointer.PlatformCaptures);
        }

        [Fact]
        public void Gesture_Recognizer_Capture_Should_Move_Platform_Capture_To_Target()
        {
            var pointer = new TestPointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            var initialCapture = new Border();
            var target = new Border { Child = initialCapture };
            var recognizer = new TestGestureRecognizer { Target = target };

            pointer.Capture(initialCapture, CaptureSource.Implicit);
            recognizer.CapturePointer(pointer);

            Assert.Null(pointer.Captured);
            Assert.Same(recognizer, pointer.CapturedGestureRecognizer);
            Assert.Equal([initialCapture, target], pointer.PlatformCaptures);
        }

        [Fact]
        public void Capture_Explicit_ShouldNotify_After_Implicit()
        {
            var pointer = new TestPointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);

            Border capture = new Border();

            List<CaptureSource> sources = new();
            capture.PointerCaptureChanging += (sender, e) =>
            {
                sources.Add(e.CaptureSource);
            };

            pointer.Capture(capture, CaptureSource.Implicit);
            pointer.Capture(capture, CaptureSource.Explicit);

            Assert.True(sources.SequenceEqual([CaptureSource.Implicit, CaptureSource.Explicit]));

            Assert.Equal(1, pointer.PlatformCaptureCalled);

            pointer.Capture(null, CaptureSource.Implicit); // not ignored, so captured element will become null
            pointer.Capture(null, CaptureSource.Explicit); // changing from null to null does not notify anything

            Assert.True(sources.SequenceEqual([CaptureSource.Implicit, CaptureSource.Explicit, CaptureSource.Implicit]));

            Assert.Equal(2, pointer.PlatformCaptureCalled);
        }

        [Fact]
        public void Capture_Explicit_ShouldNotify_After_HandledImplicit()
        {
            var pointer = new TestPointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);

            Border capture = new Border();

            List<CaptureSource> sources = new();
            capture.PointerCaptureChanging += (sender, e) =>
            {
                sources.Add(e.CaptureSource);
                e.Handled = e.CaptureSource == CaptureSource.Implicit;
            };

            pointer.Capture(capture, CaptureSource.Implicit);
            pointer.Capture(capture, CaptureSource.Explicit);

            Assert.True(sources.SequenceEqual([CaptureSource.Implicit, CaptureSource.Explicit]));

            Assert.Equal(1, pointer.PlatformCaptureCalled);

            pointer.Capture(null, CaptureSource.Implicit);
            pointer.Capture(null, CaptureSource.Explicit);
            Assert.True(sources.SequenceEqual([CaptureSource.Implicit, CaptureSource.Explicit, CaptureSource.Implicit, CaptureSource.Explicit]));

            Assert.Equal(2, pointer.PlatformCaptureCalled);
        }

        private sealed class TestGestureRecognizer : GestureRecognizer
        {
            public void CapturePointer(IPointer pointer) => Capture(pointer);

            protected override void PointerPressed(PointerPressedEventArgs e)
            {
            }

            protected override void PointerReleased(PointerReleasedEventArgs e)
            {
            }

            protected override void PointerMoved(PointerEventArgs e)
            {
            }

            protected override void PointerCaptureLost(IPointer pointer)
            {
            }
        }
    }
}
