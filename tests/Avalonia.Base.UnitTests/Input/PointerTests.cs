using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
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
    }
}
