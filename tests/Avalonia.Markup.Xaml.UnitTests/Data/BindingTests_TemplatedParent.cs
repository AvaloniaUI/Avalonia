using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Data
{
    public class BindingTests_TemplatedParent : XamlTestBase
    {
        [Fact]
        public void TemplateBinding_With_Null_Path_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Button Name='button'>
       <Button.Template>
         <ControlTemplate>
           <TextBlock Tag='{TemplateBinding}'/>
         </ControlTemplate>
       </Button.Template>
    </Button>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var button = window.FindControl<Button>("button");

                window.ApplyTemplate();
                button.ApplyTemplate();

                var textBlock = (TextBlock)button.GetVisualChildren().Single();
                Assert.Same(button, textBlock.Tag);
            }
        }

        [Fact]
        public void Binds_To_TemplatedParent_From_Non_Control()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Button Name='button'>
      <Button.Template>
        <ControlTemplate>
          <Grid>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width='{Binding RelativeSource={RelativeSource TemplatedParent}, Path=Tag}'/>
            </Grid.ColumnDefinitions>
          </Grid>
        </ControlTemplate>
      </Button.Template>
    </Button>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var button = window.FindControl<Button>("button");

                button.Tag = new GridLength(5, GridUnitType.Star);

                window.ApplyTemplate();
                button.ApplyTemplate();

                Assert.Equal(button.Tag, button.GetTemplateChildren().OfType<Grid>().First().ColumnDefinitions[0].Width);
            }
        }
    }
}
