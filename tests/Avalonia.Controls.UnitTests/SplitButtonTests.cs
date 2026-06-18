using System;
using Avalonia.Controls.UnitTests.Utils;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class SplitButtonTests : ScopedTestBase
{
    [Fact]
    public void SplitButton_CommandParameter_Does_Not_Change_While_Execution()
    {
        var target = new SplitButton();
        object? lastParamenter = "A";
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


    [Fact]
    void Should_Not_Fire_Click_Event_On_Space_Key_When_It_Is_Not_Focus()
    {
        using (UnitTestApplication.Start(TestServices.StyledWindow))
        {
            var raised = 0;
            var target = new TextBox();
            var button = new SplitButton()
            {
                Content = target,
            };

            var window = new Window { Content = button };
            window.Show();

            button.Click += (s, e) => ++raised;
            target.Focus();
            target.RaiseEvent(CreateKeyDownEvent(Key.Space));
            target.RaiseEvent(CreateKeyUpEvent(Key.Space));
            Assert.Equal(0, raised);
        }
    }

    private static KeyEventArgs CreateKeyDownEvent(Key key, Interactive? source = null)
    {
        return new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, Source = source };
    }

    private static KeyEventArgs CreateKeyUpEvent(Key key, Interactive? source = null)
    {
        return new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Key = key, Source = source };
    }
}
