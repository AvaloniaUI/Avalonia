using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class MenuItemTests
    {
        [Fact]
        public void Header_Of_Minus_Should_Apply_Separator_Pseudoclass()
        {
            var target = new MenuItem { Header = "-" };

            Assert.True(target.Classes.Contains(":separator"));
        }

        [Fact]
        public void Separator_Item_Should_Set_Focusable_False()
        {
            var target = new MenuItem { Header = "-" };

            Assert.False(target.Focusable);
        }


        [Fact]
        public void MenuItem_Is_Disabled_When_Command_Is_Enabled_But_IsEnabled_Is_False()
        {
            var command = new TestCommand(true);
            var target = new MenuItem
            {
                IsEnabled = false,
                Command = command,
            };

            var root = new TestRoot { Child = target };

            Assert.False(((IInputElement)target).IsEffectivelyEnabled);
        }

        [Fact]
        public void MenuItem_Is_Disabled_When_Bound_Command_Doesnt_Exist()
        {
            var target = new MenuItem
            {
                [!MenuItem.CommandProperty] = new Binding("Command"),
            };

            Assert.True(target.IsEnabled);
            Assert.False(target.IsEffectivelyEnabled);
        }

        [Fact]
        public void MenuItem_Is_Disabled_When_Bound_Command_Is_Removed()
        {
            var viewModel = new
            {
                Command = new TestCommand(true),
            };

            var target = new MenuItem
            {
                DataContext = viewModel,
                [!MenuItem.CommandProperty] = new Binding("Command"),
            };

            Assert.True(target.IsEnabled);
            Assert.True(target.IsEffectivelyEnabled);

            target.DataContext = null;

            Assert.True(target.IsEnabled);
            Assert.False(target.IsEffectivelyEnabled);
        }

        [Fact]
        public void MenuItem_Is_Enabled_When_Bound_Command_Is_Added()
        {
            var viewModel = new
            {
                Command = new TestCommand(true),
            };

            var target = new MenuItem
            {
                DataContext = new object(),
                [!MenuItem.CommandProperty] = new Binding("Command"),
            };

            Assert.True(target.IsEnabled);
            Assert.False(target.IsEffectivelyEnabled);

            target.DataContext = viewModel;

            Assert.True(target.IsEnabled);
            Assert.True(target.IsEffectivelyEnabled);
        }

        [Fact]
        public void MenuItem_Is_Disabled_When_Disabled_Bound_Command_Is_Added()
        {
            var viewModel = new
            {
                Command = new TestCommand(false),
            };

            var target = new MenuItem
            {
                DataContext = new object(),
                [!MenuItem.CommandProperty] = new Binding("Command"),
            };

            Assert.True(target.IsEnabled);
            Assert.False(target.IsEffectivelyEnabled);

            target.DataContext = viewModel;

            Assert.True(target.IsEnabled);
            Assert.False(target.IsEffectivelyEnabled);
        }

        [Fact]
        public void MenuItem_Does_Not_Subscribe_To_Command_CanExecuteChanged_Until_Added_To_Logical_Tree()
        {
            var command = new TestCommand();
            var target = new MenuItem
            {
                Command = command,
            };

            Assert.Equal(0, command.SubscriptionCount);
        }

        [Fact]
        public void MenuItem_Subscribes_To_Command_CanExecuteChanged_When_Added_To_Logical_Tree()
        {
            var command = new TestCommand();
            var target = new MenuItem { Command = command };
            var root = new TestRoot { Child = target };

            Assert.Equal(1, command.SubscriptionCount);
        }

        [Fact]
        public void MenuItem_Unsubscribes_From_Command_CanExecuteChanged_When_Removed_From_Logical_Tree()
        {
            var command = new TestCommand();
            var target = new MenuItem { Command = command };
            var root = new TestRoot { Child = target };

            root.Child = null;
            Assert.Equal(0, command.SubscriptionCount);
        }
        
        [Fact]
        public void MenuItem_Invokes_CanExecute_When_CommandParameter_Changed()
        {
            var command = new TestCommand(p => p is bool value && value);
            var target = new MenuItem { Command = command };

            target.CommandParameter = true;
            Assert.True(target.IsEffectivelyEnabled);

            target.CommandParameter = false;
            Assert.False(target.IsEffectivelyEnabled);
        }

        private class TestCommand : ICommand
        {
            private readonly Func<object, bool> _canExecute;
            private readonly Action<object> _execute;
            private EventHandler _canExecuteChanged;

            public TestCommand(bool enabled = true)
                : this(_ => enabled, _ => { })
            {
            }

            public TestCommand(Func<object, bool> canExecute, Action<object> execute = null)
            {
                _canExecute = canExecute;
                _execute = execute ?? (_ => { });
            }

            public int SubscriptionCount { get; private set; }

            public event EventHandler CanExecuteChanged
            {
                add { _canExecuteChanged += value; ++SubscriptionCount; }
                remove { _canExecuteChanged -= value; --SubscriptionCount; }
            }

            public bool CanExecute(object parameter) => _canExecute(parameter);

            public void Execute(object parameter) => _execute(parameter);
        }
    }
}
