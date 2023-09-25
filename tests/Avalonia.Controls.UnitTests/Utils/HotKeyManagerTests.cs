using System;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Xunit;
using Factory = System.Func<int, System.Action<object>, Avalonia.Controls.Window, Avalonia.AvaloniaObject>;

namespace Avalonia.Controls.UnitTests.Utils
{
    public class HotKeyManagerTests
    {
        [Fact]
        public void HotKeyManager_Should_Register_And_Unregister_Key_Binding()
        {
            using (AvaloniaLocator.EnterScope())
            {
                AvaloniaLocator.CurrentMutable
                    .Bind<IWindowingPlatform>().ToConstant(new MockWindowingPlatform());

                var gesture1 = new KeyGesture(Key.A, KeyModifiers.Control);
                var gesture2 = new KeyGesture(Key.B, KeyModifiers.Control);

                var tl = new Window();
                var button = new Button();
                tl.Content = button;
                tl.Template = CreateWindowTemplate();
                tl.ApplyTemplate();
                tl.Presenter.ApplyTemplate();

                HotKeyManager.SetHotKey(button, gesture1);

                Assert.Equal(gesture1, tl.KeyBindings[0].Gesture);

                HotKeyManager.SetHotKey(button, gesture2);
                Assert.Equal(gesture2, tl.KeyBindings[0].Gesture);

                tl.Content = null;
                tl.Presenter.ApplyTemplate();

                Assert.Empty(tl.KeyBindings);

                tl.Content = button;
                tl.Presenter.ApplyTemplate();

                Assert.Equal(gesture2, tl.KeyBindings[0].Gesture);

                HotKeyManager.SetHotKey(button, null);
                Assert.Empty(tl.KeyBindings);
            }
        }

        [Theory]
        [MemberData(nameof(ElementsFactory), parameters: true)]
        public void HotKeyManager_Should_Use_CommandParameter(string factoryName, Factory factory)
        {
            using (AvaloniaLocator.EnterScope())
            {
                var target = new KeyboardDevice();
                var commandResult = 0;
                var expectedParameter = 1;
                AvaloniaLocator.CurrentMutable
                    .Bind<IWindowingPlatform>().ToConstant(new MockWindowingPlatform());

                var gesture = new KeyGesture(Key.A, KeyModifiers.Control);

                var action = new Action<object>(parameter =>
                {
                    if (parameter is int value)
                    {
                        commandResult = value;
                    }
                });

                var root = new Window();
                var element = factory(expectedParameter, action, root);

                root.Template = CreateWindowTemplate();
                root.ApplyTemplate();
                root.Presenter.ApplyTemplate();

                HotKeyManager.SetHotKey(element, gesture);

                target.ProcessRawEvent(new RawKeyEventArgs(target,
                    0,
                    root,
                    RawKeyEventType.KeyDown,
                    Key.A,
                    RawInputModifiers.Control,
                    PhysicalKey.A,
                    "a"));

                Assert.True(expectedParameter == commandResult, $"{factoryName} HotKey did not carry the CommandParameter.");
            }
        }


        [Theory]
        [MemberData(nameof(ElementsFactory), parameters: true)]
        public void HotKeyManager_Should_Do_Not_Executed_When_IsEnabled_False(string factoryName, Factory factory)
        {
            using (AvaloniaLocator.EnterScope())
            {
                var target = new KeyboardDevice();
                var isExecuted = false;
                AvaloniaLocator.CurrentMutable
                    .Bind<IWindowingPlatform>().ToConstant(new MockWindowingPlatform());

                var gesture = new KeyGesture(Key.A, KeyModifiers.Control);

                var action = new Action<object>(parameter =>
                {
                    isExecuted = true;
                });

                var root = new Window();
                var element = factory(0, action, root) as InputElement;

                element.IsEnabled = false;

                root.Template = CreateWindowTemplate();
                root.ApplyTemplate();
                root.Presenter.ApplyTemplate();

                HotKeyManager.SetHotKey(element, gesture);

                target.ProcessRawEvent(new RawKeyEventArgs(target,
                    0,
                    root,
                    RawKeyEventType.KeyDown,
                    Key.A,
                    RawInputModifiers.Control,
                    PhysicalKey.A,
                    "a"));

                Assert.True(isExecuted == false, $"{factoryName} Execution raised when IsEnabled is false.");
            }
        }

        [Theory]
        [MemberData(nameof(ElementsFactory), parameters:false)]
        public void HotKeyManager_Should_Invoke_Event_Click_When_Command_Is_Null(string factoryName, Factory factory)
        {
            using (AvaloniaLocator.EnterScope())
            {
                var target = new KeyboardDevice();
                var clickExecutedCount = 0;
                AvaloniaLocator.CurrentMutable
                    .Bind<IWindowingPlatform>().ToConstant(new MockWindowingPlatform());

                var gesture = new KeyGesture(Key.A, KeyModifiers.Control);

                void Clickable_Click(object sender, Interactivity.RoutedEventArgs e)
                {
                    clickExecutedCount++;
                }

                var root = new Window();
                var element = factory(0, default, root) as InputElement;
                if (element is IClickableControl clickable)
                {
                    clickable.Click += Clickable_Click;
                }

                root.Template = CreateWindowTemplate();
                root.ApplyTemplate();
                root.Presenter.ApplyTemplate();

                HotKeyManager.SetHotKey(element, gesture);

                target.ProcessRawEvent(new RawKeyEventArgs(target,
                    0,
                    root,
                    RawKeyEventType.KeyDown,
                    Key.A,
                    RawInputModifiers.Control,
                    PhysicalKey.A,
                    "a"));

                element.IsEnabled = false;

                target.ProcessRawEvent(new RawKeyEventArgs(target,
                    0,
                    root,
                    RawKeyEventType.KeyDown,
                    Key.A,
                    RawInputModifiers.Control,
                    PhysicalKey.A,
                    "a"));


                Assert.True(clickExecutedCount == 1, $"{factoryName} Execution raised when IsEnabled is false.");
            }
        }

        [Theory]
        [MemberData(nameof(ElementsFactory), parameters: true)]
        public void HotKeyManager_Should_Not_Invoke_Event_Click_When_Command_Is_Not_Null(string factoryName, Factory factory)
        {
            using (AvaloniaLocator.EnterScope())
            {
                var target = new KeyboardDevice();
                var clickExecutedCount = 0;
                var commandExecutedCount = 0;
                AvaloniaLocator.CurrentMutable
                    .Bind<IWindowingPlatform>().ToConstant(new MockWindowingPlatform());

                var gesture = new KeyGesture(Key.A, KeyModifiers.Control);

                void DoExecute(object parameter)
                {
                    commandExecutedCount++;
                }

                void Clickable_Click(object sender, Interactivity.RoutedEventArgs e)
                {
                    clickExecutedCount++;
                }

                var root = new Window();
                var element = factory(0, DoExecute, root) as InputElement;
                if (element is IClickableControl clickable)
                {
                    clickable.Click += Clickable_Click;
                }

                root.Template = CreateWindowTemplate();
                root.ApplyTemplate();
                root.Presenter.ApplyTemplate();

                HotKeyManager.SetHotKey(element, gesture);

                target.ProcessRawEvent(new RawKeyEventArgs(target,
                    0,
                    root,
                    RawKeyEventType.KeyDown,
                    Key.A,
                    RawInputModifiers.Control,
                    PhysicalKey.A,
                    "a"));

                element.IsEnabled = false;

                target.ProcessRawEvent(new RawKeyEventArgs(target,
                    0,
                    root,
                    RawKeyEventType.KeyDown,
                    Key.A,
                    RawInputModifiers.Control,
                    PhysicalKey.A,
                    "a"));

                Assert.True(commandExecutedCount == 1, $"{factoryName} Execution raised when IsEnabled is false.");
                Assert.True(clickExecutedCount == 0, $"{factoryName} Execution raised event Click.");
            }
        }


        public static TheoryData<string, Factory> ElementsFactory(bool withCommand) =>

            new TheoryData<string, Factory>()
            {
                {nameof(Button), withCommand ? MakeButton : MakeButtonWithoutCommand},
                {nameof(MenuItem),withCommand ? MakeMenu : MakeMenuWithoutCommand},
            };

        private static AvaloniaObject MakeMenu(int expectedParameter, Action<object> action, Window root)
        {
            var menuitem = new MenuItem()
            {
                Command = new Command(action),
                CommandParameter = expectedParameter,
            };
            var rootMenu = new Menu();

            rootMenu.Items.Add(menuitem);

            root.Content = rootMenu;
            return menuitem;
        }

        private static AvaloniaObject MakeButton(int expectedParameter, Action<object> action, Window root)
        {
            var button = new Button()
            {
                Command = new Command(action),
                CommandParameter = expectedParameter,
            };

            root.Content = button;
            return button;
        }

        private static AvaloniaObject MakeMenuWithoutCommand(int expectedParameter, Action<object> action, Window root)
        {
            var menuitem = new MenuItem()
            {
            };
            var rootMenu = new Menu();

            rootMenu.Items.Add(menuitem);

            root.Content = rootMenu;
            return menuitem;
        }

        private static AvaloniaObject MakeButtonWithoutCommand(int expectedParameter, Action<object> action, Window root)
        {
            var button = new Button()
            {
            };

            root.Content = button;
            return button;
        }

        private static FuncControlTemplate CreateWindowTemplate()
        {
            return new FuncControlTemplate<Window>((parent, scope) =>
            {
                return new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [~ContentPresenter.ContentProperty] = parent[~ContentControl.ContentProperty],
                }.RegisterInNameScope(scope);
            });
        }

        class Command : System.Windows.Input.ICommand
        {
            private readonly Action<object> _execeute;

#pragma warning disable 67 // Event not used
            public event EventHandler CanExecuteChanged;
#pragma warning restore 67 // Event not used

            public Command(Action<object> execeute)
            {
                _execeute = execeute;
            }
            public bool CanExecute(object parameter) => true;

            public void Execute(object parameter) => _execeute?.Invoke(parameter);
        }
    }
}
