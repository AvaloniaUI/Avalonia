//using System.Linq;
//using Avalonia.Controls;
//using Avalonia.UnitTests;
//using Xunit;

//namespace Avalonia.Markup.Xaml.UnitTests.HotReload.Diff
//{
//    public class ChangePropertyTests : DiffTestBase
//    {
//        [Fact]
//        public void ChangeSingleProperty()
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
//  <Border Background='Green' />
//</UserControl>";

//                var diff = GetDiffBlocks<TestControl>(xaml, modifiedXaml);
                
//                Assert.Empty(diff.AddedBlocks);
//                Assert.Empty(diff.BlockMap);
//                Assert.Empty(diff.RemovedBlocks);
//                Assert.Empty(diff.AddedProperties);
//                Assert.Empty(diff.RemovedProperties);
                
//                Assert.Equal(diff.PropertyMap.Count, 1);
//                Assert.Equal(diff.PropertyMap.First().OldProperty.Parent.Type, typeof(Border).FullName);
//                Assert.Equal(diff.PropertyMap.First().NewProperty.Parent.Type, typeof(Border).FullName);
//                Assert.Equal(diff.PropertyMap.First().OldProperty.Name, "Background");
//                Assert.Equal(diff.PropertyMap.First().NewProperty.Name, "Background");
//            }
//        }
        
//        [Fact]
//        public void ChangeMultipleProperties()
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
//  <Border Background='Green' Padding='30' />
//</UserControl>";

//                var diff = GetDiffBlocks<TestControl>(xaml, modifiedXaml);
                
//                Assert.Empty(diff.AddedBlocks);
//                Assert.Empty(diff.BlockMap);
//                Assert.Empty(diff.RemovedBlocks);
//                Assert.Empty(diff.AddedProperties);
//                Assert.Empty(diff.RemovedProperties);
                
//                Assert.Equal(diff.PropertyMap.Count, 2);
//                Assert.Equal(diff.PropertyMap.First().OldProperty.Parent.Type, typeof(Border).FullName);
//                Assert.Equal(diff.PropertyMap.First().NewProperty.Parent.Type, typeof(Border).FullName);
//                Assert.Equal(diff.PropertyMap.First().OldProperty.Name, "Padding");
//                Assert.Equal(diff.PropertyMap.First().NewProperty.Name, "Padding");
                
//                Assert.Equal(diff.PropertyMap.Skip(1).First().OldProperty.Parent.Type, typeof(Border).FullName);
//                Assert.Equal(diff.PropertyMap.Skip(1).First().NewProperty.Parent.Type, typeof(Border).FullName);
//                Assert.Equal(diff.PropertyMap.Skip(1).First().OldProperty.Name, "Background");
//                Assert.Equal(diff.PropertyMap.Skip(1).First().NewProperty.Name, "Background");
//            }
//        }
        
//        [Fact]
//        public void NestedProperty()
//        {
//            using (UnitTestApplication.Start(TestServices.StyledWindow))
//            {
//                var xaml = @"
//<UserControl xmlns='https://github.com/avaloniaui'
//             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
//             x:Class='Avalonia.Markup.Xaml.UnitTests.HotReload.TestControl'>
//  <StackPanel Spacing='20'>
//    <StackPanel>
//      <TextBlock Text='Asd' Foreground='Orange' />
//    </StackPanel>

//    <ExperimentalAcrylicBorder Width='660' CornerRadius='5'>
//      <ExperimentalAcrylicBorder.Material>
//        <ExperimentalAcrylicMaterial
//          TintColor='White'
//          BackgroundSource='Digger' />
//      </ExperimentalAcrylicBorder.Material>
//    </ExperimentalAcrylicBorder>
//  </StackPanel>
//</UserControl>";

//                var modifiedXaml = xaml.Replace(
//                    "<TextBlock Text='Asd' Foreground='Orange' />",
//                    "<TextBlock Text='Asd' Foreground='Orange' Margin='10' />");

//                var diff = GetDiffBlocks<TestControl>(xaml, modifiedXaml);
                
//                Assert.Empty(diff.AddedBlocks);
//                Assert.Empty(diff.BlockMap);
//                Assert.Empty(diff.RemovedBlocks);
//                Assert.Empty(diff.PropertyMap);
//                Assert.Empty(diff.RemovedProperties);
//            }
//        }
//    }
//}
