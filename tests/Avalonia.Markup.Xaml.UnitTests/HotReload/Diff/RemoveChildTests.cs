//using Avalonia.Controls;
//using Avalonia.UnitTests;
//using Xunit;

//namespace Avalonia.Markup.Xaml.UnitTests.HotReload.Diff
//{
//    public class RemoveChildTests : DiffTestBase
//    {
//        [Fact]
//        public void RemoveOnlyChildCollection()
//        {
//            using (UnitTestApplication.Start(TestServices.StyledWindow))
//            {
//                var xaml = @"
//<UserControl xmlns='https://github.com/avaloniaui'
//             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
//             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
//  <Border Padding='20' HorizontalAlignment='Center'>
//    <StackPanel Spacing='20'>
//      <TextBlock Text='Text' />
//    </StackPanel>
//  </Border>
//</UserControl>";
                
//                var modifiedXaml = @"
//<UserControl xmlns='https://github.com/avaloniaui'
//             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
//             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
//  <Border Padding='20' HorizontalAlignment='Center'>
//    <StackPanel Spacing='20' />
//  </Border>
//</UserControl>";

//                var diff = GetDiffBlocks<TestControl>(xaml, modifiedXaml);
                
//                Assert.Empty(diff.BlockMap);
//                Assert.Empty(diff.AddedBlocks);
//                Assert.Empty(diff.PropertyMap);
//                Assert.Empty(diff.AddedProperties);
//                Assert.Empty(diff.RemovedProperties);
                
//                Assert.Equal(diff.RemovedBlocks.Count, 1);
//                Assert.Equal(diff.RemovedBlocks[0].Type, typeof(TextBlock).FullName);
//                Assert.Equal(diff.RemovedBlocks[0].Properties.Count, 1);
//                Assert.Equal(diff.RemovedBlocks[0].Properties[0].Name, "Text");
//            }
//        }
        
//        [Fact]
//        public void RemoveOnlyChildDirect()
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
//  <Border Padding='20' HorizontalAlignment='Center' />
//</UserControl>";

//                var diff = GetDiffBlocks<TestControl>(xaml, modifiedXaml);
                
//                Assert.Empty(diff.BlockMap);
//                Assert.Empty(diff.AddedBlocks);
//                Assert.Empty(diff.PropertyMap);
//                Assert.Empty(diff.AddedProperties);
//                Assert.Empty(diff.RemovedBlocks);
                
//                Assert.Equal(diff.RemovedProperties.Count, 1);
//                Assert.Equal(diff.RemovedProperties[0].Type, typeof(Decorator).FullName);
//                Assert.Equal(diff.RemovedProperties[0].Name, nameof(Decorator.Child));
//            }
//        }
        
//        [Fact]
//        public void RemoveFirstChildWithSameTypeSiblings()
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
//      <TextBlock Text='Text' />
//    </StackPanel>
//  </Border>
//</UserControl>";
                
//                var modifiedXaml = @"
//<UserControl xmlns='https://github.com/avaloniaui'
//             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
//             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
//  <Border Padding='20' HorizontalAlignment='Center'>
//    <StackPanel Spacing='20'>
//      <TextBlock Text='Text' />
//    </StackPanel>
//  </Border>
//</UserControl>";

//                var diff = GetDiffBlocks<TestControl>(xaml, modifiedXaml);

//                Assert.Empty(diff.BlockMap);
//                Assert.Empty(diff.AddedBlocks);
//                Assert.Empty(diff.PropertyMap);
//                Assert.Empty(diff.AddedProperties);
//                Assert.Empty(diff.RemovedProperties);

//                Assert.Equal(diff.RemovedBlocks.Count, 1);
//                Assert.Equal(diff.RemovedBlocks[0].Type, typeof(TextBlock).FullName);
//                Assert.Equal(diff.RemovedBlocks[0].Properties.Count, 2);
//                Assert.Equal(diff.RemovedBlocks[0].Properties[0].Name, "Text");
//                Assert.Equal(diff.RemovedBlocks[0].Properties[1].Name, "Foreground");
//            }
//        }
        
//        [Fact]
//        public void RemoveChildThatHasChildren()
//        {
//            using (UnitTestApplication.Start(TestServices.StyledWindow))
//            {
//                var xaml = @"
//<UserControl xmlns='https://github.com/avaloniaui'
//             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
//             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
//  <StackPanel HorizontalAlignment='Center'>
//    <StackPanel Spacing='20'>
//      <TextBlock Text='Text' />
//      <TextBlock Text='Text' />
//    </StackPanel>
//  </StackPanel>
//</UserControl>";
                
//                var modifiedXaml = @"
//<UserControl xmlns='https://github.com/avaloniaui'
//             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
//             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
//  <StackPanel HorizontalAlignment='Center' />
//</UserControl>";

//                var diff = GetDiffBlocks<TestControl>(xaml, modifiedXaml);

//                Assert.Empty(diff.BlockMap);
//                Assert.Empty(diff.AddedBlocks);
//                Assert.Empty(diff.PropertyMap);
//                Assert.Empty(diff.AddedProperties);
//                Assert.Empty(diff.RemovedProperties);

//                Assert.Equal(diff.RemovedBlocks.Count, 1);
//                Assert.Equal(diff.RemovedBlocks[0].Type, typeof(StackPanel).FullName);
//                Assert.True(diff.RemovedBlocks[0].Properties.Count > 0);
//                Assert.Equal(diff.RemovedBlocks[0].Properties[0].Name, "Spacing");
//            }
//        }
//    }
//}
