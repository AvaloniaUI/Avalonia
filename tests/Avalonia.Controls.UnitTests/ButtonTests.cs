using System;
using System.Windows.Input;
using Avalonia.Markup.Xaml.Data;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ButtonTests
    {
        [Fact]
        public void Button_Is_Disabled_When_Command_Is_Disabled()
        {
            var command = new TestCommand(false);
            var target = new Button
            {
                Command = command,
            };

            Assert.False(target.IsEnabled);
            command.IsEnabled = true;
            Assert.True(target.IsEnabled);
            command.IsEnabled = false;
            Assert.False(target.IsEnabled);
        }

        [Fact]
        public void Button_Is_Disabled_When_Bound_Command_Doesnt_Exist()
        {
            var target = new Button
            {
                [!Button.CommandProperty] = new Binding("Command"),
            };

            Assert.False(target.IsEnabled);
        }

        [Fact]
        public void Button_Is_Disabled_When_Bound_Command_Is_Removed()
        {
            var viewModel = new
            {
                Command = new TestCommand(true),
            };

            var target = new Button
            {
                DataContext = viewModel,
                [!Button.CommandProperty] = new Binding("Command"),
            };

            Assert.True(target.IsEnabled);
            target.DataContext = null;
            Assert.False(target.IsEnabled);
        }

        [Fact]
        public void Button_Is_Enabled_When_Bound_Command_Is_Added()
        {
            var viewModel = new
            {
                Command = new TestCommand(true),
            };

            var target = new Button
            {
                DataContext = new object(),
                [!Button.CommandProperty] = new Binding("Command"),
            };

            Assert.False(target.IsEnabled);
            target.DataContext = viewModel;
            Assert.True(target.IsEnabled);
        }

        [Fact]
        public void Button_Is_Disabled_When_Disabled_Bound_Command_Is_Added()
        {
            var viewModel = new
            {
                Command = new TestCommand(false),
            };

            var target = new Button
            {
                DataContext = new object(),
                [!Button.CommandProperty] = new Binding("Command"),
            };

            Assert.False(target.IsEnabled);
            target.DataContext = viewModel;
            Assert.False(target.IsEnabled);
        }

        private class TestCommand : ICommand
        {
            private bool _enabled;

            public TestCommand(bool enabled)
            {
                _enabled = enabled;
            }

            public bool IsEnabled
            {
                get { return _enabled; }
                set
                {
                    if (_enabled != value)
                    {
                        _enabled = value;
                        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
            }

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter) => _enabled;

            public void Execute(object parameter)
            {
            }
        }
    }
}