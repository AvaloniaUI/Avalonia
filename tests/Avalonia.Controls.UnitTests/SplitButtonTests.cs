using System;
using Avalonia.Controls.UnitTests.Utils;
using Avalonia.Input;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class SplitButtonTests : ScopedTestBase
{
    [Fact]
    public void SplitButton_CommandParameter_Does_Not_Change_While_Execution()
    {
        var target = new SplitButton();
        object lastParamenter = "A";
        var generator = new Random();
        var command = new TestCommand(parameter =>
        {
            target.CommandParameter = generator.Next();
            lastParamenter = parameter;
            return true;
        },
        parameter =>
        {
            Assert.Equal(lastParamenter, parameter);
        });
        target.CommandParameter = lastParamenter;
        target.Command = command;
        var root = new TestRoot { Child = target };

        (target as IClickableControl).RaiseClick();
    }
}
