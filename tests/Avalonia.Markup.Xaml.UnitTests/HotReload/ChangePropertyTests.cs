using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.HotReload
{
    public class ChangePropertyTests : HotReloadTestBase
    {
        [Fact]
        public void Binding_To_DataContext_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
  <Border Padding='20' HorizontalAlignment='Center'>
    <StackPanel Spacing='20' Orientation='Vertical'>
    </StackPanel>
  </Border>
</UserControl>";
                
                var modifiedXaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
  <Border Padding='40' HorizontalAlignment='Stretch'>
    <StackPanel Spacing='40' Orientation='Horizontal' />
  </Border>
</UserControl>";
                
                var (original, modified) = ParseAndApplyHotReload<TestControl>(xaml, modifiedXaml);

                var originalBorder = (Border)original.Content;
                var modifiedBorder = (Border)modified.Content;
                
                var originalPanel = (StackPanel)originalBorder.Child;
                var modifiedPanel = (StackPanel)modifiedBorder.Child;
                
                Assert.Equal(originalBorder.Padding, modifiedBorder.Padding);
                Assert.Equal(originalBorder.HorizontalAlignment, modifiedBorder.HorizontalAlignment);
                
                Assert.Equal(originalPanel.Spacing, modifiedPanel.Spacing);
                Assert.Equal(originalPanel.Orientation, modifiedPanel.Orientation);
                
                Assert.Equal(originalPanel.Children.Count, modifiedPanel.Children.Count);
            }
        }
    }
}
