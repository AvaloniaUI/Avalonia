using System.Reflection;
using Avalonia.Controls.PullToRefresh;
using Avalonia.Input;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.PullToRefresh
{
    public class ScrollablePullGestureRecognizerTests : ScopedTestBase
    {
        // Repro for the "PointerCaptureLost doesn't clean up state" bug.
        //
        // PointerReleased clears _pullInProgress, _tracking, _initialPosition.
        // PointerCaptureLost only raises EndPull (so the PullGestureEnded event fires)
        // but leaves _pullInProgress == true and _tracking pointing at the lost pointer.
        // The next gesture re-enters PointerMoved with (_pullInProgress=true, pulling=true)
        // and goes straight to HandlePull, skipping BeginPull. This reuses the OLD _gestureId
        // for the next PullGestureEvent - the same id that was just used in the
        // PullGestureEndedEvent for the previous gesture.
        [Fact]
        public void PointerCaptureLost_resets_recognizer_state_like_PointerReleased()
        {
            var recognizer = new ScrollablePullGestureRecognizer(PullDirection.TopToBottom, isMouseEnabled: true);

            var pullInProgressField = GetField("_pullInProgress");
            var trackingField = GetField("_tracking");
            var initialPositionField = GetField("_initialPosition");

            // Simulate the recognizer being mid-pull with a tracked pointer
            var pointer = new Avalonia.Input.Pointer(Avalonia.Input.Pointer.GetNextFreeId(), PointerType.Touch, isPrimary: true);
            pullInProgressField.SetValue(recognizer, true);
            trackingField.SetValue(recognizer, pointer);
            initialPositionField.SetValue(recognizer, new Point(10, 20));

            // Capture is lost (e.g. another control steals it, or the visual is detached mid-gesture)
            InvokePointerCaptureLost(recognizer, pointer);

            // The recognizer must be in a fully-clean state, like after PointerReleased.
            Assert.False((bool)pullInProgressField.GetValue(recognizer)!,
                "_pullInProgress must be cleared after PointerCaptureLost");
            Assert.Null(trackingField.GetValue(recognizer));
            Assert.Equal(default(Point), (Point)initialPositionField.GetValue(recognizer)!);
        }

        private static FieldInfo GetField(string name)
        {
            var f = typeof(ScrollablePullGestureRecognizer)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(f);
            return f!;
        }

        private static void InvokePointerCaptureLost(ScrollablePullGestureRecognizer recognizer, IPointer pointer)
        {
            var method = typeof(ScrollablePullGestureRecognizer)
                .GetMethod("PointerCaptureLost", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            method!.Invoke(recognizer, new object[] { pointer });
        }
    }
}
