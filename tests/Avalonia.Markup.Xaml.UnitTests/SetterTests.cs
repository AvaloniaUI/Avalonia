using System.Linq;
using Avalonia.Data;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests
{
    public class SetterTests : XamlTestBase
    {
        [Fact]
        public void Setter_Should_Work_Outside_Of_Style_With_SetterTargetType_Attribute()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Animation xmlns='https://github.com/avaloniaui' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:SetterTargetType='Avalonia.Controls.Button'>
    <KeyFrame>
        <Setter Property='Content' Value='{Binding}'/>
    </KeyFrame>
</Animation>";
                var animation = (Animation.Animation)AvaloniaRuntimeXamlLoader.Load(xaml);
                var setter = (Setter)animation.Children[0].Setters[0];

                Assert.IsType<Binding>(setter.Value);
            }
        }
    }
}
