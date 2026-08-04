using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Input
{
    public class AccessKeyHandlerTests : ScopedTestBase
    {
        [Fact]
        public void Should_Raise_Key_Events_For_Unregistered_Access_Key()
        {
            var root = new TestRoot();
            var target = new AccessKeyHandler();
            var events = new List<string>();

            target.SetOwner(root);
            root.KeyDown += (s, e) => events.Add($"KeyDown {e.Key}");
            root.KeyUp += (s, e) => events.Add($"KeyUp {e.Key}");

            KeyDown(root, Key.LeftAlt);
            KeyDown(root, Key.A, "a", KeyModifiers.Alt);
            KeyUp(root, Key.A, "a", KeyModifiers.Alt);
            KeyUp(root, Key.LeftAlt);

            Assert.Equal(new[]
            {
                "KeyDown LeftAlt",
                "KeyDown A",
                "KeyUp A",
                "KeyUp LeftAlt",
            }, events);
        }

        [Fact]
        public void Should_Raise_Key_Events_For_Unregistered_Access_Key_With_MainMenu()
        {
            var root = new TestRoot();
            var target = new AccessKeyHandler();
            var menu = Mock.Of<IMainMenu>();
            var events = new List<string>();

            target.SetOwner(root);
            target.MainMenu = menu;
            root.KeyDown += (s, e) => events.Add($"KeyDown {e.Key}");
            root.KeyUp += (s, e) => events.Add($"KeyUp {e.Key}");

            KeyDown(root, Key.LeftAlt);
            KeyDown(root, Key.A, "a", KeyModifiers.Alt);
            KeyUp(root, Key.A, "a", KeyModifiers.Alt);
            KeyUp(root, Key.LeftAlt);

            Assert.Equal(new[]
            {
                "KeyDown LeftAlt",
                "KeyDown A",
                "KeyUp A",
                "KeyUp LeftAlt",
            }, events);
        }

        [Fact]
        public void Should_Raise_Key_Events_For_Alt_Key()
        {
            var root = new TestRoot();
            var target = new AccessKeyHandler();
            var events = new List<string>();

            target.SetOwner(root);
            root.KeyDown += (s, e) => events.Add($"KeyDown {e.Key}");
            root.KeyUp += (s, e) => events.Add($"KeyUp {e.Key}");

            KeyDown(root, Key.LeftAlt);
            KeyUp(root, Key.LeftAlt);

            Assert.Equal(new[]
            {
                "KeyDown LeftAlt",
                "KeyUp LeftAlt",
            }, events);
        }

        [Fact]
        public void Should_Raise_Key_Events_For_Alt_Key_With_MainMenu()
        {
            var root = new TestRoot();
            var target = new AccessKeyHandler();
            var menu = new Mock<IMainMenu>();
            var events = new List<string>();

            menu.SetupAllProperties();
            menu.Setup(x => x.Open()).Callback(() => menu.Setup(x => x.IsOpen).Returns(true));

            target.SetOwner(root);
            target.MainMenu = menu.Object;

            root.KeyDown += (s, e) => events.Add($"KeyDown {e.Key}");
            root.KeyUp += (s, e) => events.Add($"KeyUp {e.Key}");

            KeyDown(root, Key.LeftAlt);
            KeyUp(root, Key.LeftAlt);
            KeyDown(root, Key.LeftAlt);
            KeyUp(root, Key.LeftAlt);

            Assert.Equal(new[]
            {
                "KeyDown LeftAlt",
                "KeyUp LeftAlt",
                "KeyDown LeftAlt",
                "KeyUp LeftAlt",
            }, events);
        }

        [Fact]
        public void Should_Raise_Key_Events_For_Registered_Access_Key()
        {
            var button = new Button();
            var root = new TestRoot(button);
            var target = new AccessKeyHandler();
            var events = new List<string>();

            target.SetOwner(root);
            target.Register("A", button);
            root.KeyDown += (s, e) => events.Add($"KeyDown {e.Key}");
            root.KeyUp += (s, e) => events.Add($"KeyUp {e.Key}");

            KeyDown(root, Key.LeftAlt);
            KeyDown(root, Key.A, "a", KeyModifiers.Alt);
            KeyUp(root, Key.A, "a", KeyModifiers.Alt);
            KeyUp(root, Key.LeftAlt);

            // Alt+A's KeyDown is now correctly matched and marked Handled by
            // AccessKeyHandler.OnKeyDown (Bubble), so it no longer reaches a plain
            // (non-handledEventsToo) KeyDown subscriber. The corresponding KeyUp is
            // unaffected: AccessKeyHandler only reacts to KeyUp for the Alt key itself
            // (OnPreviewKeyUp), not for arbitrary registered access keys.
            Assert.Equal(new[]
            {
                "KeyDown LeftAlt",
                "KeyUp A",
                "KeyUp LeftAlt",
            }, events);
        }

        [Theory]
        [InlineData("A", Key.A, "a")]
        [InlineData("A", Key.Q, "a")]
        [InlineData("é", Key.D2, "é")]
        [InlineData("2", Key.D2, "2")]
        public void Should_Raise_AccessKey_For_Registered_Access_Key_Matching_KeySymbol(string registered, Key key, string keySymbol)
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var button = new Button();
                var root = new TestRoot(button);
                var target = new AccessKeyHandler();
                var raised = 0;

                KeyboardDevice.Instance?.SetFocusedElement(button, NavigationMethod.Unspecified, KeyModifiers.None);

                target.SetOwner(root);
                target.Register(registered, button);
                button.AddHandler(AccessKeyHandler.AccessKeyEvent, (s, e) => ++raised);

                KeyDown(root, Key.LeftAlt);
                Assert.Equal(0, raised);

                KeyDown(root, key, keySymbol, KeyModifiers.Alt);
                Assert.Equal(1, raised);

                KeyUp(root, key, keySymbol, KeyModifiers.Alt);
                KeyUp(root, Key.LeftAlt);

                Assert.Equal(1, raised);
            }
        }

        [Fact]
        public void Should_Not_Raise_AccessKey_For_Registered_Access_Key_Not_Matching_KeySymbol()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var button = new Button();
                var root = new TestRoot(button);
                var target = new AccessKeyHandler();
                var raised = 0;

                KeyboardDevice.Instance?.SetFocusedElement(button, NavigationMethod.Unspecified, KeyModifiers.None);

                target.SetOwner(root);
                target.Register("A", button);
                button.AddHandler(AccessKeyHandler.AccessKeyEvent, (_, _) => ++raised);

                KeyDown(root, Key.LeftAlt);
                Assert.Equal(0, raised);

                KeyDown(root, Key.A, "q", KeyModifiers.Alt);
                Assert.Equal(0, raised);

                KeyUp(root, Key.A, "q", KeyModifiers.Alt);
                KeyUp(root, Key.LeftAlt);

                Assert.Equal(0, raised);
            }
        }

        [Theory]
        [InlineData(false, 0)]
        [InlineData(true, 1)]
        public void Should_Raise_AccessKey_For_Registered_Access_Key_When_Effectively_Enabled(bool enabled, int expected)
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var button = new Button();
                var root = new TestRoot(button) { IsEnabled = enabled };
                var target = new AccessKeyHandler();
                var raised = 0;
                
                KeyboardDevice.Instance?.SetFocusedElement(button, NavigationMethod.Unspecified, KeyModifiers.None);
                
                target.SetOwner(root);
                target.Register("A", button);
                button.AddHandler(AccessKeyHandler.AccessKeyEvent, (s, e) => ++raised);

                KeyDown(root, Key.LeftAlt);
                Assert.Equal(0, raised);

                KeyDown(root, Key.A, "a", KeyModifiers.Alt);
                Assert.Equal(expected, raised);

                KeyUp(root, Key.A, "a", KeyModifiers.Alt);
                KeyUp(root, Key.LeftAlt);
                Assert.Equal(expected, raised);
            }
        }

        [Fact]
        public void Should_Raise_AccessKey_For_System_Key_Event_With_KeySymbol()
        {
            // Regression test for #20961: on Windows, WM_SYSKEYDOWN (Alt+key) previously
            // left KeySymbol null, breaking access keys. MapVirtualKey now provides KeySymbol.
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var button = new Button();
                var root = new TestRoot(button);
                var target = new AccessKeyHandler();
                var raised = 0;

                KeyboardDevice.Instance?.SetFocusedElement(button, NavigationMethod.Unspecified, KeyModifiers.None);

                target.SetOwner(root);
                target.Register("F", button);
                button.AddHandler(AccessKeyHandler.AccessKeyEvent, (s, e) => ++raised);

                KeyDown(root, Key.LeftAlt);
                Assert.Equal(0, raised);

                // MapVirtualKey provides lowercase KeySymbol for system key events
                KeyDown(root, Key.F, "f", KeyModifiers.Alt);
                Assert.Equal(1, raised);
            }
        }

        [Fact]
        public void Should_Open_MainMenu_On_Alt_KeyUp()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target = new AccessKeyHandler();
                var menu = new FakeMenu();
                var root = new TestRoot(menu);

                KeyboardDevice.Instance?.SetFocusedElement(menu, NavigationMethod.Unspecified,
                    KeyModifiers.None);

                target.SetOwner(root);
                target.MainMenu = menu;

                KeyDown(root, Key.LeftAlt);
                Assert.Equal(0, menu.TimesOpenCalled);
                
                KeyUp(root, Key.LeftAlt);
                Assert.Equal(1, menu.TimesOpenCalled);
            }
        }

        [Fact]
        public void Should_Raise_AccessKey_When_Nothing_Is_Focused()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var button = new Button();
                var root = new TestRoot(button);
                var target = new AccessKeyHandler();
                var raised = 0;

                Assert.Null(KeyboardDevice.Instance?.FocusedElement);

                target.SetOwner(root);
                target.Register("A", button);
                button.AddHandler(AccessKeyHandler.AccessKeyEvent, (s, e) => ++raised);

                KeyDown(root, Key.LeftAlt);
                KeyDown(root, Key.A, "a", KeyModifiers.Alt);

                Assert.Equal(1, raised);
            }
        }

        [Fact]
        public void Should_Raise_AccessKey_When_Owner_Itself_Is_Focused()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var button = new Button();
                var root = new TestRoot(button);
                var target = new AccessKeyHandler();
                var raised = 0;

                KeyboardDevice.Instance?.SetFocusedElement(root, NavigationMethod.Unspecified, KeyModifiers.None);

                target.SetOwner(root);
                target.Register("A", button);
                button.AddHandler(AccessKeyHandler.AccessKeyEvent, (s, e) => ++raised);

                KeyDown(root, Key.LeftAlt);
                KeyDown(root, Key.A, "a", KeyModifiers.Alt);

                Assert.Equal(1, raised);
            }
        }

        [Fact]
        public void Should_Open_MainMenu_On_Alt_KeyUp_When_Nothing_Is_Focused()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target = new AccessKeyHandler();
                var menu = new FakeMenu();
                var root = new TestRoot(menu);

                Assert.Null(KeyboardDevice.Instance?.FocusedElement);

                target.SetOwner(root);
                target.MainMenu = menu;

                KeyDown(root, Key.LeftAlt);
                Assert.Equal(0, menu.TimesOpenCalled);

                KeyUp(root, Key.LeftAlt);
                Assert.Equal(1, menu.TimesOpenCalled);
            }
        }

        private static void KeyDown(IInputElement target, Key key, string? keySymbol = null, KeyModifiers modifiers = KeyModifiers.None)
        {
            target.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = key,
                KeySymbol = keySymbol,
                KeyModifiers = modifiers,
            });
        }

        private static void KeyUp(IInputElement target, Key key, string? keySymbol = null, KeyModifiers modifiers = KeyModifiers.None)
        {
            target.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyUpEvent,
                Key = key,
                KeySymbol = keySymbol,
                KeyModifiers = modifiers,
            });
        }
        
        class FakeMenu : Menu
        {
            public int TimesOpenCalled { get; set; }
            
            public override void Open()
            {
                TimesOpenCalled++;
            }
        }
    }
}
