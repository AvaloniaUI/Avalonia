using System;
using System.Windows.Input;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class NativeMenuItemTests : ScopedTestBase
{
    [Fact]
    public void Setting_CommandParameter_After_Command_Should_Enable_Item()
    {
        var target = new NativeMenuItem { Command = new IntParameterCommand() };

        Assert.False(target.IsEnabled);

        target.CommandParameter = 123;

        Assert.True(target.IsEnabled);
    }

    [Fact]
    public void Clearing_CommandParameter_Should_Disable_Item()
    {
        var target = new NativeMenuItem { CommandParameter = 123, Command = new IntParameterCommand() };

        Assert.True(target.IsEnabled);

        target.CommandParameter = null;

        Assert.False(target.IsEnabled);
    }

    [Fact]
    public void Setting_CommandParameter_Without_Command_Should_Not_Enable_Item()
    {
        var target = new NativeMenuItem { IsEnabled = false };

        target.CommandParameter = 123;

        Assert.False(target.IsEnabled);
    }

    private sealed class IntParameterCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => parameter is int;

        public void Execute(object? parameter)
        {
        }
    }
}
