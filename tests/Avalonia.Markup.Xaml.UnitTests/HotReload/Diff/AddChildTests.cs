//using Avalonia.Controls;
//using Avalonia.UnitTests;
//using Xunit;

//namespace Avalonia.Markup.Xaml.UnitTests.HotReload.Diff
//{
//    public class AddChildTests : DiffTestBase
//    {
//        [Fact]
//        public void AddChildToEmptyParent()
//        {
//            using (UnitTestApplication.Start(TestServices.StyledWindow))
//            {
//                var xaml = @"
//<UserControl xmlns='https://github.com/avaloniaui'
//             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
//             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
//  <Border Padding='20' HorizontalAlignment='Center'>
//    <StackPanel Spacing='20' />
//  </Border>
//</UserControl>";
                
//                var modifiedXaml = @"
//<UserControl xmlns='https://github.com/avaloniaui'
//             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
//             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
//  <Border Padding='20' HorizontalAlignment='Center'>
//    <StackPanel Spacing='20'>
//      <TextBlock Text='TintOpacity' Foreground='Black' />
//    </StackPanel>
//  </Border>
//</UserControl>";

//                var diff = GetDiffBlocks<TestControl>(xaml, modifiedXaml);
                
//                Assert.Empty(diff.BlockMap);
//                Assert.Empty(diff.RemovedBlocks);
//                Assert.Empty(diff.PropertyMap);
//                Assert.Empty(diff.AddedProperties);
//                Assert.Empty(diff.RemovedProperties);
                
//                Assert.Equal(diff.AddedBlocks.Count, 1);
//                Assert.Equal(diff.AddedBlocks[0].Type, typeof(TextBlock).FullName);
//                Assert.Equal(diff.AddedBlocks[0].Properties.Count, 2);
//                Assert.Equal(diff.AddedBlocks[0].Properties[0].Name, "Text");
//                Assert.Equal(diff.AddedBlocks[0].Properties[1].Name, "Foreground");
//            }
//        }
        
//        [Fact]
//        public void AddChildToNonEmptyParentWithDifferentProperties()
//        {
//            using (UnitTestApplication.Start(TestServices.StyledWindow))
//            {
//                var xaml = @"
//<UserControl xmlns='https://github.com/avaloniaui'
//             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
//             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
//  <Border Padding='20' HorizontalAlignment='Center'>
//    <StackPanel Spacing='20'>
//      <TextBlock Text='TintOpacity' Foreground='Black' />
//    </StackPanel>
//  </Border>
//</UserControl>";
                
//                var modifiedXaml = @"
//<UserControl xmlns='https://github.com/avaloniaui'
//             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
//             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
//  <Border Padding='20' HorizontalAlignment='Center'>
//    <StackPanel Spacing='20'>
//      <TextBlock Text='TintOpacity' Foreground='Black' />
//      <TextBlock Text='TintOpacity' />
//    </StackPanel>
//  </Border>
//</UserControl>";

//                var diff = GetDiffBlocks<TestControl>(xaml, modifiedXaml);
                
//                Assert.Empty(diff.BlockMap);
//                Assert.Empty(diff.RemovedBlocks);
//                Assert.Empty(diff.PropertyMap);
//                Assert.Empty(diff.AddedProperties);
//                Assert.Empty(diff.RemovedProperties);
                
//                Assert.Equal(diff.AddedBlocks.Count, 1);
//                Assert.Equal(diff.AddedBlocks[0].Type, typeof(TextBlock).FullName);
//                Assert.Equal(diff.AddedBlocks[0].Properties.Count, 1);
//                Assert.Equal(diff.AddedBlocks[0].Properties[0].Name, "Text");
//            }
//        }
//    }
//}
