using System;
using System.Threading;
using OpenQA.Selenium.Interactions;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class GestureTests : TestBase
    {
        public GestureTests(DefaultAppFixture fixture)
            : base(fixture, "Gestures")
        {
            var clear = Session.FindElementByAccessibilityId("ResetGestures");
            clear.Click();
        }

        [PlatformFact(TestPlatforms.Windows | TestPlatforms.MacOS)]
        public void Tapped_Is_Raised()
        {
            TappedIsRaisedCore();
        }

        private void TappedIsRaisedCore()
        {
            var border = Session.FindElementByAccessibilityId("GestureBorder");
            var lastGesture = Session.FindElementByAccessibilityId("LastGesture");

            new Actions(Session).Click(border).Perform();

            Assert.Equal("Tapped", lastGesture.Text);
        }

        [PlatformFact(TestPlatforms.Windows | TestPlatforms.MacOS)]
        public void Tapped_Is_Raised_Slow()
        {
            TappedIsRaisedSlowCore();
        }

        private void TappedIsRaisedSlowCore()
        {
            var border = Session.FindElementByAccessibilityId("GestureBorder");
            var lastGesture = Session.FindElementByAccessibilityId("LastGesture");

            new Actions(Session).ClickAndHold(border).Perform();

            Thread.Sleep(2000);

            new Actions(Session).Release(border).Perform();

            Assert.Equal("Tapped", lastGesture.Text);
        }

        [PlatformFact(TestPlatforms.Windows | TestPlatforms.MacOS)]
        public void Tapped_Is_Not_Raised_For_Drag()
        {
            TappedIsNotRaisedForDragCore();
        }

        private void TappedIsNotRaisedForDragCore()
        {
            var border = Session.FindElementByAccessibilityId("GestureBorder");
            var lastGesture = Session.FindElementByAccessibilityId("LastGesture");

            new Actions(Session)
                .ClickAndHold(border)
                .MoveByOffset(50, 50)
                .Release()
                .Perform();

            Assert.Equal(string.Empty, lastGesture.Text);
        }

        [PlatformFact(TestPlatforms.Windows | TestPlatforms.MacOS)]
        public void DoubleTapped_Is_Raised()
        {
            DoubleTappedIsRaisedCore();
        }

        private void DoubleTappedIsRaisedCore()
        {
            var border = Session.FindElementByAccessibilityId("GestureBorder");
            var lastGesture = Session.FindElementByAccessibilityId("LastGesture");

            new Actions(Session).DoubleClick(border).Perform();

            Assert.Equal("DoubleTapped", lastGesture.Text);
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void DoubleTapped_Is_Raised_2()
        {
            DoubleTappedIsRaised2Core();
        }

        private void DoubleTappedIsRaised2Core()
        {
            var border = Session.FindElementByAccessibilityId("GestureBorder");
            var lastGesture = Session.FindElementByAccessibilityId("LastGesture");

            new Actions(Session).ClickAndHold(border).Release().Perform();

            Thread.Sleep(50);

            // DoubleTapped is raised on second pointer press, not release.
            new Actions(Session).ClickAndHold(border).Perform();

            try
            {
                Assert.Equal("DoubleTapped", lastGesture.Text);
            }
            finally
            {
                
                new Actions(Session).MoveToElement(lastGesture).Release().Perform();
            }
        }

        [PlatformFact(TestPlatforms.Linux)]
        public void Linux_Tapped_Is_Raised()
        {
            using var fixture = new DefaultAppFixture();
            var isolated = new GestureTests(fixture);
            isolated.TappedIsRaisedCore();
        }

        [PlatformFact(TestPlatforms.Linux)]
        public void Linux_Tapped_Is_Raised_Slow()
        {
            using var fixture = new DefaultAppFixture();
            var isolated = new GestureTests(fixture);
            isolated.TappedIsRaisedSlowCore();
        }

        [PlatformFact(TestPlatforms.Linux)]
        public void Linux_Tapped_Is_Not_Raised_For_Drag()
        {
            using var fixture = new DefaultAppFixture();
            var isolated = new GestureTests(fixture);
            isolated.TappedIsNotRaisedForDragCore();
        }

        [PlatformFact(TestPlatforms.Linux)]
        public void Linux_DoubleTapped_Is_Raised()
        {
            using var fixture = new DefaultAppFixture();
            var isolated = new GestureTests(fixture);
            isolated.DoubleTappedIsRaisedCore();
        }

        [PlatformFact(TestPlatforms.Linux)]
        public void Linux_DoubleTapped_Is_Raised_2()
        {
            using var fixture = new DefaultAppFixture();
            var isolated = new GestureTests(fixture);
            isolated.DoubleTappedIsRaised2Core();
        }

        [PlatformFact(TestPlatforms.Linux)]
        public void Linux_DoubleTapped_Is_Raised_Not_Raised_If_Too_Slow()
        {
            using var fixture = new DefaultAppFixture();
            var isolated = new GestureTests(fixture);
            isolated.DoubleTappedIsRaisedNotRaisedIfTooSlowCore();
        }

        [PlatformFact(TestPlatforms.Linux)]
        public void Linux_DoubleTapped_Is_Raised_After_Control_Changes()
        {
            using var fixture = new DefaultAppFixture();
            var isolated = new GestureTests(fixture);
            isolated.DoubleTappedIsRaisedAfterControlChangesCore();
        }

        [PlatformFact(TestPlatforms.Windows | TestPlatforms.MacOS)]
        public void DoubleTapped_Is_Raised_Not_Raised_If_Too_Slow()
        {
            DoubleTappedIsRaisedNotRaisedIfTooSlowCore();
        }

        private void DoubleTappedIsRaisedNotRaisedIfTooSlowCore()
        {
            var border = Session.FindElementByAccessibilityId("GestureBorder");
            var lastGesture = Session.FindElementByAccessibilityId("LastGesture");

            new Actions(Session).ClickAndHold(border).Release().Perform();

            Thread.Sleep(2000);

            new Actions(Session).ClickAndHold(border).Release().Perform();

            Assert.Equal("Tapped", lastGesture.Text);
        }

        [PlatformFact(TestPlatforms.Windows | TestPlatforms.MacOS)]
        public void DoubleTapped_Is_Raised_After_Control_Changes()
        {
            DoubleTappedIsRaisedAfterControlChangesCore();
        }

        private void DoubleTappedIsRaisedAfterControlChangesCore()
        {
            // #8733
            var border = Session.FindElementByAccessibilityId("GestureBorder");
            var lastGesture = Session.FindElementByAccessibilityId("LastGesture");
            
            new Actions(Session)
                .MoveToElement(border)
                .DoubleClick()
                .Perform();
            
            Thread.Sleep(100);
            
            new Actions(Session).MoveToElement(lastGesture, 200, 200).DoubleClick().Perform();

            Assert.Equal("DoubleTapped2", lastGesture.Text);
        }

        [PlatformFact(TestPlatforms.Windows | TestPlatforms.MacOS)]
        public void RightTapped_Is_Raised()
        {
            RightTappedIsRaisedCore();
        }

        private void RightTappedIsRaisedCore()
        {
            var border = Session.FindElementByAccessibilityId("GestureBorder");
            var lastGesture = Session.FindElementByAccessibilityId("LastGesture");

            new Actions(Session).ContextClick(border).Perform();

            Assert.Equal("RightTapped", lastGesture.Text);
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void RightTapped_Is_Raised_2()
        {
            var border = Session.FindElementByAccessibilityId("GestureBorder");
            var lastGesture = Session.FindElementByAccessibilityId("LastGesture");
            var device = new PointerInputDevice(PointerKind.Mouse);
            var b = new ActionBuilder();

            b.AddAction(device.CreatePointerMove(border, 50, 50, TimeSpan.FromMilliseconds(50)));
            b.AddAction(device.CreatePointerDown(MouseButton.Right));
            b.AddAction(device.CreatePointerMove(border, 52, 52, TimeSpan.FromMilliseconds(50)));
            b.AddAction(device.CreatePointerUp(MouseButton.Right));
            Session.PerformActions(b.ToActionSequenceList());

            Assert.Equal("RightTapped", lastGesture.Text);
        }

        [PlatformFact(TestPlatforms.Linux)]
        public void Linux_RightTapped_Is_Raised()
        {
            using var fixture = new DefaultAppFixture();
            var isolated = new GestureTests(fixture);
            isolated.RightTappedIsRaisedCore();
        }

        [PlatformFact(TestPlatforms.Linux)]
        public void Linux_RightTapped_Is_Raised_2()
        {
            using var fixture = new DefaultAppFixture();
            var isolated = new GestureTests(fixture);
            isolated.RightTapped_Is_Raised_2();
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void RightTapped_Is_Not_Raised_For_Drag()
        {
            var border = Session.FindElementByAccessibilityId("GestureBorder");
            var lastGesture = Session.FindElementByAccessibilityId("LastGesture");
            var device = new PointerInputDevice(PointerKind.Mouse);
            var b = new ActionBuilder();

            b.AddAction(device.CreatePointerMove(border, 50, 50, TimeSpan.FromMilliseconds(100)));
            b.AddAction(device.CreatePointerDown(MouseButton.Right));
            b.AddAction(device.CreatePointerMove(CoordinateOrigin.Pointer, 50, 50, TimeSpan.FromMilliseconds(100)));
            b.AddAction(device.CreatePointerUp(MouseButton.Right));

            Assert.Equal(string.Empty, lastGesture.Text);
        }

        [PlatformFact(TestPlatforms.Linux)]
        public void Linux_RightTapped_Is_Not_Raised_For_Drag()
        {
            using var fixture = new DefaultAppFixture();
            var isolated = new GestureTests(fixture);
            isolated.RightTapped_Is_Not_Raised_For_Drag();
        }
    }
}
