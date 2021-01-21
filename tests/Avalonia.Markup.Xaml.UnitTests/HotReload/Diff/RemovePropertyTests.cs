//using Avalonia.Controls;
//using Avalonia.UnitTests;
//using Xunit;

//namespace Avalonia.Markup.Xaml.UnitTests.HotReload.Diff
//{
//    public class RemovePropertyTests : DiffTestBase
//    {
//        [Fact]
//        public void RemoveSingleProperties()
//        {
//            using (UnitTestApplication.Start(TestServices.StyledWindow))
//            {
//                var xaml = @"
//<UserControl xmlns='https://github.com/avaloniaui'
//             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
//             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
//  <Border Background='Yellow' />
//</UserControl>";
                
//                var modifiedXaml = @"
//<UserControl xmlns='https://github.com/avaloniaui'
//             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
//             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
//  <Border />
//</UserControl>";

//                var diff = GetDiffBlocks<TestControl>(xaml, modifiedXaml);
                
//                Assert.Empty(diff.AddedBlocks);
//                Assert.Empty(diff.BlockMap);
//                Assert.Empty(diff.RemovedBlocks);
//                Assert.Empty(diff.PropertyMap);
//                Assert.Empty(diff.AddedProperties);
                
//                Assert.Equal(diff.RemovedProperties.Count, 1);
//                Assert.Equal(diff.RemovedProperties[0].Parent.Type, typeof(Border).FullName);
//                Assert.Equal(diff.RemovedProperties[0].Name, "Background");
//            }
//        }
        
//        [Fact]
//        public void RemoveMultipleProperties()
//        {
//            using (UnitTestApplication.Start(TestServices.StyledWindow))
//            {
//                var xaml = @"
//<UserControl xmlns='https://github.com/avaloniaui'
//             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
//             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
//  <Border Background='Yellow' Padding='20' />
//</UserControl>";

//                var modifiedXaml = @"
//<UserControl xmlns='https://github.com/avaloniaui'
//             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
//             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
//  <Border />
//</UserControl>";

//                var diff = GetDiffBlocks<TestControl>(xaml, modifiedXaml);

//                Assert.Empty(diff.AddedBlocks);
//                Assert.Empty(diff.BlockMap);
//                Assert.Empty(diff.RemovedBlocks);
//                Assert.Empty(diff.PropertyMap);
//                Assert.Empty(diff.AddedProperties);

//                Assert.Equal(diff.RemovedProperties.Count, 2);
//                Assert.Equal(diff.RemovedProperties[0].Parent.Type, typeof(Border).FullName);
//                Assert.Equal(diff.RemovedProperties[1].Parent.Type, typeof(Border).FullName);
//                Assert.Equal(diff.RemovedProperties[0].Name, "Background");
//                Assert.Equal(diff.RemovedProperties[1].Name, "Padding");
//            }
//        }
        
//        [Fact]
//        public void RemovePropertyWithSameTypeSiblings()
//        {
//            using (UnitTestApplication.Start(TestServices.StyledWindow))
//            {
//                var xaml = @"
//<UserControl xmlns='https://github.com/avaloniaui'
//             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
//             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
//  <StackPanel>
//    <TextBlock Text='Text' Foreground='Yellow' />
//    <TextBlock Text='Text' Foreground='Yellow' />
//  </StackPanel>
//</UserControl>";
                
//                var modifiedXaml = @"
//<UserControl xmlns='https://github.com/avaloniaui'
//             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
//             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
//  <StackPanel>
//    <TextBlock Text='Text' />
//    <TextBlock Text='Text' Foreground='Yellow' />
//  </StackPanel>
//</UserControl>";

//                var diff = GetDiffBlocks<TestControl>(xaml, modifiedXaml);
                
//                Assert.Empty(diff.AddedBlocks);
//                Assert.Empty(diff.BlockMap);
//                Assert.Empty(diff.RemovedBlocks);
//                Assert.Empty(diff.PropertyMap);
//                Assert.Empty(diff.AddedProperties);
                
//                Assert.Equal(diff.RemovedProperties.Count, 1);
//                Assert.Equal(diff.RemovedProperties[0].Parent.Type, typeof(TextBlock).FullName);
//                Assert.Equal(diff.RemovedProperties[0].Parent.ParentIndex, 0);
//                Assert.Equal(diff.RemovedProperties[0].Name, "Foreground");
//            }
//        }
//    }
//}
