using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests;

public class SetterTests : XamlTestBase
{
    [Fact]
    public void SetterTargetType_Should_Understand_xType_Extensions()
    {
        using (UnitTestApplication.Start(TestServices.StyledWindow))
        {
            var xaml = @"
<Animation xmlns='https://github.com/avaloniaui' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:SetterTargetType='{x:Type ContentControl}'>
    <KeyFrame>
        <Setter Property='Content' Value='{Binding}'/>
    </KeyFrame>
    <KeyFrame>
        <Setter Property='Content' Value='{Binding}'/>
    </KeyFrame> 
</Animation>";
            var animation = (Animation.Animation)AvaloniaRuntimeXamlLoader.Load(xaml);
            var setter = (Setter)animation.Children[0].Setters[0];

            Assert.Equal(typeof(ContentControl), setter.Property.OwnerType);
        }
    }

    [Fact]
    public void SetterTargetType_Should_Understand_Type_From_Xmlns()
    {
        using (UnitTestApplication.Start(TestServices.StyledWindow))
        {
            var xaml = @"
<av:Animation xmlns:av='https://github.com/avaloniaui' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:SetterTargetType='av:ContentControl'>
    <av:KeyFrame>
        <av:Setter Property='Content' Value='{av:Binding}'/>
    </av:KeyFrame>
    <av:KeyFrame>
        <av:Setter Property='Content' Value='{av:Binding}'/>
    </av:KeyFrame> 
</av:Animation>";
            var animation = (Animation.Animation)AvaloniaRuntimeXamlLoader.Load(xaml);
            var setter = (Setter)animation.Children[0].Setters[0];

            Assert.Equal(typeof(ContentControl), setter.Property.OwnerType);
        }
    }

    [Theory]
    [InlineData("{x:Static InputElement.KeyDownEvent}","OnKeyDown")]
    [InlineData("KeyDown","OnKeyDown")]
    [InlineData("KeyDown", "OnKeyDownUnspecific")]
    [InlineData("KeyDown","OnKeyDownStatic")]
    public void EventSetter_Should_Be_Registered(string eventName, string handlerName)
    {
        using (UnitTestApplication.Start(TestServices.StyledWindow))
        {
            var window = (WindowWithEventHandler)AvaloniaRuntimeXamlLoader.Load($"""
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.WindowWithEventHandler'>
    <Window.Styles>
        <Style Selector='Window'>
            <EventSetter Event='{eventName}'
                         Handler='{handlerName}' />
        </Style>
    </Window.Styles>
</Window>
""");
            var callbackCalled = false;
            window.Callback += _ => callbackCalled = true;

            window.Show();
            
            window.RaiseEvent(new KeyEventArgs() { RoutedEvent = InputElement.KeyDownEvent });

            Assert.True(callbackCalled);
        }
    }
}

public class WindowWithEventHandler : Window
{
    public Action<RoutedEventArgs> Callback;
    public void OnKeyDown(object sender, KeyEventArgs e)
    {
        Callback?.Invoke(e);
    }
    public void OnKeyDownUnspecific(object sender, RoutedEventArgs e)
    {
        Callback?.Invoke(e);
    }
    public static void OnKeyDownStatic(object sender, RoutedEventArgs e)
    {
        ((WindowWithEventHandler)sender).Callback?.Invoke(e);
    }
}
