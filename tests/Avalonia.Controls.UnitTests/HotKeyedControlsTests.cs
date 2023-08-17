using System;
using System.Windows.Input;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.LogicalTree;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    internal class HotKeyedTextBox : TextBox, ICommandSource
    {
        private class DelegateCommand : ICommand
        {
            private readonly Action _action;
            public DelegateCommand(Action action) => _action = action;
            public event EventHandler CanExecuteChanged { add { } remove { } }
            public bool CanExecute(object parameter) => true;
            public void Execute(object parameter) => _action();
        }
        
        public static readonly StyledProperty<KeyGesture> HotKeyProperty =
            HotKeyManager.HotKeyProperty.AddOwner<HotKeyedTextBox>();

        private KeyGesture _hotkey;

        public KeyGesture HotKey
        {
            get => GetValue(HotKeyProperty);
            set => SetValue(HotKeyProperty, value);
        }

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            if (_hotkey != null)
            {
                this.SetValue(HotKeyProperty, _hotkey);
            }

            base.OnAttachedToLogicalTree(e);
        }

        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            if (this.HotKey != null)
            {
                _hotkey = this.HotKey;
                this.SetValue(HotKeyProperty, null);
            }

            base.OnDetachedFromLogicalTree(e);
        }

        public void CanExecuteChanged(object sender, EventArgs e)
        {
        }

        protected override Type StyleKeyOverride => typeof(TextBox);

        public ICommand Command => _command;

        public object CommandParameter => null;

        private readonly DelegateCommand _command;

        public HotKeyedTextBox()
        {
            _command = new DelegateCommand(() => Focus());
        }
    }

    public class HotKeyedControlsTests
    {
        private static Window PreparedWindow(object content = null)
        {
            var platform = AvaloniaLocator.Current.GetRequiredService<IWindowingPlatform>();
            var windowImpl = Mock.Get(platform.CreateWindow());
            windowImpl.Setup(x => x.Compositor).Returns(RendererMocks.CreateDummyCompositor());
            var w = new Window(windowImpl.Object) { Content = content };
            w.ApplyTemplate();
            return w;
        }
        
        private static IDisposable CreateServicesWithFocus()
        {
            return UnitTestApplication.Start(
                TestServices.StyledWindow.With(
                    windowingPlatform: new MockWindowingPlatform(
                        null,
                        window => MockWindowingPlatform.CreatePopupMock(window).Object),
                focusManager: new FocusManager(),
                keyboardDevice: () => new KeyboardDevice()));
        }
        
        [Fact]
        public void HotKeyedTextBox_Focus_Performed_On_Hotkey()
        {
            using var _ = CreateServicesWithFocus();
            
            var keyboardDevice = new KeyboardDevice();
            var hotKeyedTextBox = new HotKeyedTextBox { HotKey = new KeyGesture(Key.F, KeyModifiers.Control) };
            var root = PreparedWindow();
            root.Content = hotKeyedTextBox;
            root.Show();

            Assert.False(hotKeyedTextBox.IsFocused);

            keyboardDevice.ProcessRawEvent(
                new RawKeyEventArgs(
                    keyboardDevice,
                    0,
                    root,
                    RawKeyEventType.KeyDown,
                    Key.F,
                    RawInputModifiers.Control,
                    PhysicalKey.F,
                    "f"));
            
            Assert.True(hotKeyedTextBox.IsFocused);
        }
    }
}
