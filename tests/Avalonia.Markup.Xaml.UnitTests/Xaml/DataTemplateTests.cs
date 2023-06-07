using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;
using Avalonia.Metadata;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class DataTemplateTests : XamlTestBase
    {
        [Fact]
        public void DataTemplate_Can_Be_Empty()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:sys='clr-namespace:System;assembly=netstandard'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.DataTemplates>
        <DataTemplate DataType='{x:Type sys:String}' />
    </Window.DataTemplates>
    <ContentControl Name='target' Content='Foo'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = window.FindControl<ContentControl>("target");

                window.ApplyTemplate();
                target.ApplyTemplate();
                target.Presenter.UpdateChild();

                Assert.Null(target.Presenter.Child);
            }
        }

        [Fact]
        public void DataTemplate_Can_Contain_Name()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:sys='clr-namespace:System;assembly=netstandard'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.DataTemplates>
        <DataTemplate DataType='{x:Type sys:String}'>
            <Canvas Name='foo'/>
        </DataTemplate>
    </Window.DataTemplates>
    <ContentControl Name='target' Content='Foo'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = window.FindControl<ContentControl>("target");

                window.ApplyTemplate();
                target.ApplyTemplate();
                target.Presenter.UpdateChild();

                Assert.IsType<Canvas>(target.Presenter.Child);
            }
        }

        [Fact]
        public void DataTemplate_Can_Contain_Named_UserControl()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:sys='clr-namespace:System;assembly=mscorlib'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ItemsControl Name='itemsControl' ItemsSource='{Binding}'>
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <UserControl Name='foo'/>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var itemsControl = window.FindControl<ItemsControl>("itemsControl");

                window.DataContext = new[] { "item1", "item2" };

                window.ApplyTemplate();
                itemsControl.ApplyTemplate();
                itemsControl.Presenter.ApplyTemplate();

                Assert.Equal(2, itemsControl.Presenter.Panel.Children.Count);
            }
        }

        [Fact]
        public void XDataType_Should_Be_Assigned_To_Clr_Property()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:sys='clr-namespace:System;assembly=netstandard'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.DataTemplates>
        <DataTemplate x:DataType='sys:String'>
            <Canvas Name='foo'/>
        </DataTemplate>
    </Window.DataTemplates>
    <ContentControl Name='target' Content='Foo'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = window.FindControl<ContentControl>("target");
                var template = (DataTemplate)window.DataTemplates.First();
                
                window.ApplyTemplate();
                target.ApplyTemplate();
                target.Presenter.UpdateChild();
                
                Assert.Equal(typeof(string), template.DataType);
                Assert.IsType<Canvas>(target.Presenter.Child);
            }
        }
        
        [Fact]
        public void XDataType_Should_Be_Ignored_If_DataType_Already_Set()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:sys='clr-namespace:System;assembly=netstandard'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.DataTemplates>
        <DataTemplate DataType='sys:String' x:DataType='UserControl'>
            <Canvas Name='foo'/>
        </DataTemplate>
    </Window.DataTemplates>
    <ContentControl Name='target' Content='Foo'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = window.FindControl<ContentControl>("target");

                window.ApplyTemplate();
                target.ApplyTemplate();
                target.Presenter.UpdateChild();

                Assert.IsType<Canvas>(target.Presenter.Child);
            }
        }
        
        [Fact]
        public void XDataType_Should_Be_Ignored_If_DataType_Has_Non_Standard_Name()
        {
            // We don't want DataType to be mapped to FancyDataType, avoid possible confusion.
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:sys='clr-namespace:System;assembly=netstandard'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <ContentControl Name='target' Content='Foo'>
        <ContentControl.ContentTemplate>
            <local:CustomDataTemplate x:DataType='local:TestDataContext'>
                <TextBlock Text='{CompiledBinding StringProperty}' Name='textBlock' />
            </local:CustomDataTemplate>
        </ContentControl.ContentTemplate>
    </ContentControl>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = window.FindControl<ContentControl>("target");
                
                window.ApplyTemplate();
                target.ApplyTemplate();
                target.Presenter.UpdateChild();

                var dataTemplate = (CustomDataTemplate)target.ContentTemplate;
                Assert.Null(dataTemplate.FancyDataType);
            }
        }
        
        [Fact]
        public void Can_Set_DataContext_In_DataTemplate()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Window.DataTemplates>
        <DataTemplate DataType='{x:Type local:TestViewModel}'>
            <Canvas Name='foo' DataContext='{Binding Child}'/>
        </DataTemplate>
    </Window.DataTemplates>
    <ContentControl Name='target' Content='{Binding Child}'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = window.FindControl<ContentControl>("target");

                var viewModel = new TestViewModel
                {
                    String = "Root",
                    Child = new TestViewModel
                    {
                        String = "Child",
                        Child = new TestViewModel
                        {
                            String = "Grandchild",
                        }
                    },
                };

                window.DataContext = viewModel;

                window.ApplyTemplate();
                target.ApplyTemplate();
                target.Presenter.UpdateChild();

                var canvas = (Canvas)target.Presenter.Child;
                Assert.Same(viewModel, target.DataContext);
                Assert.Same(viewModel.Child, target.Presenter.DataContext);
                Assert.Same(viewModel.Child.Child, canvas.DataContext);
            }
        }
        
        [Fact]
        public void DataTemplates_Without_Type_Should_Throw()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:sys='clr-namespace:System;assembly=netstandard'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.DataTemplates>
        <DataTemplate>
            <Canvas Name='foo'/>
        </DataTemplate>
    </Window.DataTemplates>
    <ContentControl Name='target' Content='Foo'/>
</Window>";
                Assert.Throws<InvalidOperationException>(() => (Window)AvaloniaRuntimeXamlLoader.Load(xaml));
            }
        }
    }
}
