using Avalonia.Controls;
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
}
