using System;
using System.Threading;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class GestureTests
    {
        private readonly AppiumDriver<AppiumWebElement> _session;

        public GestureTests(DefaultAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElementByName("Gestures");
            tab.Click();
            var clear = _session.FindElementByAccessibilityId("ResetGestures");
            clear.Click();
        }

        [Fact]
        public void Tapped_Is_Raised()
        {
            var border = _session.FindElementByAccessibilityId("GestureBorder");
            var lastGesture = _session.FindElementByAccessibilityId("LastGesture");

            new Actions(_session).Click(border).Perform();

            Assert.Equal("Tapped", lastGesture.Text);
        }

        [Fact]
        public void Tapped_Is_Raised_Slow()
        {
            var border = _session.FindElementByAccessibilityId("GestureBorder");
            var lastGesture = _session.FindElementByAccessibilityId("LastGesture");

            new Actions(_session).ClickAndHold(border).Perform();

            Thread.Sleep(2000);

            new Actions(_session).Release(border).Perform();

            Assert.Equal("Tapped", lastGesture.Text);
        }

        [Fact]
        public void Tapped_Is_Not_Raised_For_Drag()
        {
            var border = _session.FindElementByAccessibilityId("GestureBorder");
            var lastGesture = _session.FindElementByAccessibilityId("LastGesture");

            new Actions(_session)
                .ClickAndHold(border)
                .MoveByOffset(50, 50)
                .Release()
                .Perform();

            Assert.Equal(string.Empty, lastGesture.Text);
        }

        [Fact]
        public void DoubleTapped_Is_Raised()
        {
            var border = _session.FindElementByAccessibilityId("GestureBorder");
            var lastGesture = _session.FindElementByAccessibilityId("LastGesture");

            new Actions(_session).DoubleClick(border).Perform();

            Assert.Equal("DoubleTapped", lastGesture.Text);
        }

        [PlatformFact(TestPlatforms.Windows | TestPlatforms.Linux)]
        public void DoubleTapped_Is_Raised_2()
        {
            var border = _session.FindElementByAccessibilityId("GestureBorder");
            var lastGesture = _session.FindElementByAccessibilityId("LastGesture");

            new Actions(_session).ClickAndHold(border).Release().Perform();

            Thread.Sleep(50);

            // DoubleTapped is raised on second pointer press, not release.
            new Actions(_session).ClickAndHold(border).Perform();

            try
            {
                Assert.Equal("DoubleTapped", lastGesture.Text);
            }
            finally
            {
                
                new Actions(_session).MoveToElement(lastGesture).Release().Perform();
            }
        }

        [Fact]
        public void DoubleTapped_Is_Raised_Not_Raised_If_Too_Slow()
        {
            var border = _session.FindElementByAccessibilityId("GestureBorder");
            var lastGesture = _session.FindElementByAccessibilityId("LastGesture");

            new Actions(_session).ClickAndHold(border).Release().Perform();

            Thread.Sleep(2000);

            new Actions(_session).ClickAndHold(border).Release().Perform();

            Assert.Equal("Tapped", lastGesture.Text);
        }

        [Fact]
        public void DoubleTapped_Is_Raised_After_Control_Changes()
        {
            // #8733
            var border = _session.FindElementByAccessibilityId("GestureBorder");
            var lastGesture = _session.FindElementByAccessibilityId("LastGesture");
            
            new Actions(_session)
                .MoveToElement(border)
                .DoubleClick()
                .Perform();
            
            Thread.Sleep(100);
            
            new Actions(_session).MoveToElement(lastGesture, 200, 200).DoubleClick().Perform();

            Assert.Equal("DoubleTapped2", lastGesture.Text);
        }

        [Fact]
        public void RightTapped_Is_Raised()
        {
            var border = _session.FindElementByAccessibilityId("GestureBorder");
            var lastGesture = _session.FindElementByAccessibilityId("LastGesture");

            new Actions(_session).ContextClick(border).Perform();

            Assert.Equal("RightTapped", lastGesture.Text);
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void RightTapped_Is_Raised_2()
        {
            var border = _session.FindElementByAccessibilityId("GestureBorder");
            var lastGesture = _session.FindElementByAccessibilityId("LastGesture");
            var device = new PointerInputDevice(PointerKind.Mouse);
            var b = new ActionBuilder();

            b.AddAction(device.CreatePointerMove(border, 50, 50, TimeSpan.FromMilliseconds(50)));
            b.AddAction(device.CreatePointerDown(MouseButton.Right));
            b.AddAction(device.CreatePointerMove(border, 52, 52, TimeSpan.FromMilliseconds(50)));
            b.AddAction(device.CreatePointerUp(MouseButton.Right));
            _session.PerformActions(b.ToActionSequenceList());

            Assert.Equal("RightTapped", lastGesture.Text);
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void RightTapped_Is_Not_Raised_For_Drag()
        {
            var border = _session.FindElementByAccessibilityId("GestureBorder");
            var lastGesture = _session.FindElementByAccessibilityId("LastGesture");
            var device = new PointerInputDevice(PointerKind.Mouse);
            var b = new ActionBuilder();

            b.AddAction(device.CreatePointerMove(border, 50, 50, TimeSpan.FromMilliseconds(100)));
            b.AddAction(device.CreatePointerDown(MouseButton.Right));
            b.AddAction(device.CreatePointerMove(CoordinateOrigin.Pointer, 50, 50, TimeSpan.FromMilliseconds(100)));
            b.AddAction(device.CreatePointerUp(MouseButton.Right));

            Assert.Equal(string.Empty, lastGesture.Text);
        }
    }
}
