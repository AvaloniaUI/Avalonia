#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.Input;
using Avalonia.Markup.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Metadata;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.MarkupExtensions
{
    public class CompiledBindingExtensionTests : XamlTestBase
    {
        static CompiledBindingExtensionTests()
        {
            RuntimeHelpers.RunClassConstructor(typeof(RelativeSource).TypeHandle);
        }

        [Fact]
        public void ResolvesClrPropertyBasedOnDataContextType()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding StringProperty}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.StringProperty, textBlock.Text);
            }
        }

        [Fact]
        public void ResolvesClrPropertyBasedOnDataContextType_InterfaceInheritance()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:IHasPropertyDerived'>
    <TextBlock Text='{CompiledBinding StringProperty}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.StringProperty, textBlock.Text);
            }
        }

        [Fact]
        public void ResolvesPathPassedByProperty()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding Path=StringProperty}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.StringProperty, textBlock.Text);
            }
        }
        
        [Fact]
        public void ResolvesPathPassedByPropertyWithInnerItemTemplate()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <ItemsControl Name='itemsControl' ItemsSource='{CompiledBinding Path=ListProperty}'>
	    <ItemsControl.ItemTemplate>
		    <DataTemplate>
			    <TextBlock />
		    </DataTemplate>
	    </ItemsControl.ItemTemplate>
    </ItemsControl>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<ItemsControl>("itemsControl");

                var dataContext = new TestDataContext
                {
                    ListProperty =
                    {
                        "Hello"
                    } 
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.ListProperty, textBlock.ItemsSource);
            }
        }
        
        
        [Fact]
        public void ResolvesStaticClrPropertyBased()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding StaticProperty}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");
                textBlock.DataContext = new TestDataContext();

                Assert.Equal(TestDataContext.StaticProperty, textBlock.Text);
            }
        }
        
        [Fact]
        public void ResolvesDataTypeFromBindingProperty()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <TextBlock Text='{CompiledBinding StringProperty, DataType=local:TestDataContext}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.StringProperty, textBlock.Text);
            }
        }
        
        [Fact]
        public void ResolvesDataTypeFromBindingProperty_TypeExtension()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <TextBlock Text='{CompiledBinding StringProperty, DataType={x:Type local:TestDataContext}}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.StringProperty, textBlock.Text);
            }
        }

        [Fact]
        public void ResolvesStreamTaskBindingCorrectly()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding TaskProperty^}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    TaskProperty = Task.FromResult("foobar")
                };

                window.DataContext = dataContext;

                Assert.Equal("foobar", textBlock.Text);
            }
        }

        [Fact]
        public void ResolvesStreamObservableBindingCorrectly()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding ObservableProperty^}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                DelayedBinding.ApplyBindings(textBlock);

                var subject = new Subject<string>();
                var dataContext = new TestDataContext
                {
                    ObservableProperty = subject
                };

                window.DataContext = dataContext;

                subject.OnNext("foobar");

                Assert.Equal("foobar", textBlock.Text);
            }
        }

        [Fact]
        public void ResolvesIndexerBindingCorrectly()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding ListProperty[3]}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    ListProperty = { "A", "B", "C", "D", "E" }
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.ListProperty[3], textBlock.Text);
            }
        }

        [Fact]
        public void IndexerSetterBindsCorrectly()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBox Text='{CompiledBinding ListProperty[3], Mode=TwoWay}' Name='textBox' />
</Window>");
                var textBox = window.GetControl<TextBox>("textBox");

                var dataContext = new TestDataContext
                {
                    ListProperty = { "A", "B", "C", "D", "E" }
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.ListProperty[3], textBox.Text);

                textBox.Text = "Z";

                Assert.Equal("Z", dataContext.ListProperty[3]);
                Assert.Equal(dataContext.ListProperty[3], textBox.Text);
            }
        }

        [Fact]
        public void ResolvesArrayIndexerBindingCorrectly()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding ArrayProperty[3]}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    ArrayProperty = new[] { "A", "B", "C", "D", "E" }
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.ArrayProperty[3], textBlock.Text);
            }
        }

        [Fact]
        public void ResolvesObservableIndexerBindingCorrectly()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding ObservableCollectionProperty[3]}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    ObservableCollectionProperty = { "A", "B", "C", "D", "E" }
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.ObservableCollectionProperty[3], textBlock.Text);

                dataContext.ObservableCollectionProperty[3] = "New Value";

                Assert.Equal(dataContext.ObservableCollectionProperty[3], textBlock.Text);
            }
        }

        [Fact]
        public void InfersCompiledBindingDataContextFromDataContextBinding()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock DataContext='{CompiledBinding StringProperty}' Text='{CompiledBinding}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                window.ApplyTemplate();
                window.Presenter!.ApplyTemplate();

                var dataContext = new TestDataContext
                {
                    StringProperty = "A"
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.StringProperty, textBlock.Text);
            }
        }

        [Fact]
        public void ResolvesNonIntegerIndexerBindingCorrectly()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding NonIntegerIndexerProperty[Test]}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext();

                dataContext.NonIntegerIndexerProperty["Test"] = "Initial Value";

                window.DataContext = dataContext;

                Assert.Equal(dataContext.NonIntegerIndexerProperty["Test"], textBlock.Text);

                dataContext.NonIntegerIndexerProperty["Test"] = "New Value";

                Assert.Equal(dataContext.NonIntegerIndexerProperty["Test"], textBlock.Text);
            }
        }

        [Fact]
        public void ResolvesNonIntegerIndexerBindingFromParentInterfaceCorrectly()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding NonIntegerIndexerInterfaceProperty[Test]}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext();

                dataContext.NonIntegerIndexerInterfaceProperty["Test"] = "Initial Value";

                window.DataContext = dataContext;

                Assert.Equal(dataContext.NonIntegerIndexerInterfaceProperty["Test"], textBlock.Text);

                dataContext.NonIntegerIndexerInterfaceProperty["Test"] = "New Value";

                Assert.Equal(dataContext.NonIntegerIndexerInterfaceProperty["Test"], textBlock.Text);
            }
        }

        [Fact]
        public void InfersDataTemplateTypeFromDataTypeProperty()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <Window.DataTemplates>
        <DataTemplate DataType='{x:Type x:String}'>
            <TextBlock Text='{CompiledBinding}' Name='textBlock' />
        </DataTemplate>
    </Window.DataTemplates>
    <ContentControl Name='target' Content='{CompiledBinding StringProperty}' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = window.GetControl<ContentControl>("target");

                var dataContext = new TestDataContext();

                dataContext.StringProperty = "Initial Value";

                window.DataContext = dataContext;

                window.ApplyTemplate();
                target.ApplyTemplate();
                target.Presenter!.UpdateChild();

                Assert.Equal(dataContext.StringProperty, ((TextBlock)target.Presenter.Child!).Text);
            }
        }


        [Fact]
        public void ThrowsOnUninferrableLooseDataTemplateNoDataTypeWithCompiledBindingPath()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <Window.DataTemplates>
        <DataTemplate>
            <TextBlock Text='{CompiledBinding StringProperty}' Name='textBlock' />
        </DataTemplate>
    </Window.DataTemplates>
    <ContentControl Name='target' Content='{CompiledBinding}' />
</Window>";
                Assert.ThrowsAny<XmlException>(() => AvaloniaRuntimeXamlLoader.Load(xaml));
            }
        }

        [Fact]
        public void ThrowsOnUninferrableDataTypeFromNonCompiledDataContextBindingWithCompiledBindingPath()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <ContentControl Name='target' DataContext='{Binding}'>
        <TextBlock Text='{CompiledBinding StringProperty}' Name='textBlock' />
    </ContentControl>
</Window>";
                Assert.ThrowsAny<XmlException>(() => AvaloniaRuntimeXamlLoader.Load(xaml));
            }
        }

        [Fact]
        public void ReportsMultipleErrorsOnDataContextAndBindingPathErrors()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <ContentControl Content='{CompiledBinding NoDataContext}'
                    Tag='{CompiledBinding NonExistentProp, DataType=local:TestDataContext}'
                    Height='{CompiledBinding invalid.}' />
</Window>";
                var ex = Assert.Throws<AggregateException>(() => AvaloniaRuntimeXamlLoader.Load(xaml));
                Assert.Collection(
                    ex.InnerExceptions,
                    inner => Assert.IsAssignableFrom<XmlException>(inner),
                    inner => Assert.IsAssignableFrom<XmlException>(inner),
                    inner => Assert.IsAssignableFrom<XmlException>(inner));
            }
        }

        [Fact]
        public void InfersDataTemplateTypeFromParentCollectionItemsType()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <ItemsControl ItemsSource='{CompiledBinding ListProperty}' Name='target'>
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <TextBlock Text='{CompiledBinding}' Name='textBlock' />
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = window.GetControl<ItemsControl>("target");

                var dataContext = new TestDataContext();

                dataContext.ListProperty.Add("Test");

                window.DataContext = dataContext;

                window.ApplyTemplate();
                target.ApplyTemplate();
                target.Presenter!.ApplyTemplate();

                Assert.Equal(dataContext.ListProperty[0], (string?)((ContentPresenter)target.Presenter.Panel!.Children[0]).Content);
            }
        }

////        [Fact]
////        public void InfersDataTypeFromParentDataGridItemsTypeInCaseOfControlInheritance()
////        {
////            using (UnitTestApplication.Start(TestServices.StyledWindow))
////            {
////                var window = (Window)AvaloniaRuntimeXamlLoader.Load(@"
////<Window xmlns='https://github.com/avaloniaui'
////        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
////        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
////        x:DataType='local:TestItemsCollectionDataContext'>
////    <local:DataGridLikeControlInheritor Items='{CompiledBinding Items}' Name='target'>
////        <local:DataGridLikeControlInheritor.Columns>
////            <local:DataGridLikeColumn Binding='{CompiledBinding StringProperty}'>
////            </local:DataGridLikeColumn>
////        </local:DataGridLikeControlInheritor.Columns>
////    </local:DataGridLikeControlInheritor>
////</Window>");
////                var target = window.GetControl<DataGridLikeControl>("target");
////                var column = target.Columns.Single();

////                var dataContext = new TestItemsCollectionDataContext();

////                dataContext.Items.Add(new TestData() { StringProperty = "Test" });

////                window.DataContext = dataContext;

////                window.ApplyTemplate();
////                target.ApplyTemplate();

////                // Assert DataGridLikeColumn.Binding data type.
////                var compiledPath = ((CompiledBindingExtension)column.Binding!).Path;
////                var node = Assert.IsType<PropertyElement>(Assert.Single(compiledPath.Elements));

////                Assert.Equal(typeof(string), node.Property.PropertyType);
////                Assert.Equal(nameof(TestData.StringProperty), node.Property.Name);
////            }
////        }

////        [Fact]
////        public void InfersDataTemplateTypeFromParentDataGridItemsType()
////        {
////            using (UnitTestApplication.Start(TestServices.StyledWindow))
////            {
////                var window = (Window)AvaloniaRuntimeXamlLoader.Load(@"
////<Window xmlns='https://github.com/avaloniaui'
////        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
////        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
////        x:DataType='local:TestDataContext'>
////    <local:DataGridLikeControl Items='{CompiledBinding ListProperty}' Name='target'>
////        <local:DataGridLikeControl.Columns>
////            <local:DataGridLikeColumn Binding='{CompiledBinding Length}'>
////                <local:DataGridLikeColumn.Template>
////                    <DataTemplate>
////                        <TextBlock Text='{CompiledBinding Length}' />
////                    </DataTemplate>
////                </local:DataGridLikeColumn.Template>
////            </local:DataGridLikeColumn>
////        </local:DataGridLikeControl.Columns>
////    </local:DataGridLikeControl>
////</Window>");
////                var target = window.GetControl<DataGridLikeControl>("target");
////                var column = target.Columns.Single();

////                var dataContext = new TestDataContext();

////                dataContext.ListProperty.Add("Test");

////                window.DataContext = dataContext;

////                window.ApplyTemplate();
////                target.ApplyTemplate();

////                // Assert DataGridLikeColumn.Binding data type.
////                var compiledPath = ((CompiledBindingExtension)column.Binding!).Path;
////                var node = Assert.IsType<PropertyElement>(Assert.Single(compiledPath.Elements));
////                Assert.Equal(typeof(int), node.Property.PropertyType);
                
////                // Assert DataGridLikeColumn.Template data type by evaluating the template.
////                var firstItem = dataContext.ListProperty[0];
////                var textBlockFromTemplate = (TextBlock)column.Template!.Build(firstItem)!;
////                textBlockFromTemplate.DataContext = firstItem;
////                Assert.Equal(firstItem.Length.ToString(), textBlockFromTemplate.Text);
////            }
////        }
        
////        [Fact]
////        public void ExplicitDataTypeStillWorksOnDataGridLikeControls()
////        {
////            using (UnitTestApplication.Start(TestServices.StyledWindow))
////            {
////                var window = (Window)AvaloniaRuntimeXamlLoader.Load(@"
////<Window xmlns='https://github.com/avaloniaui'
////        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
////        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
////        x:DataType='local:TestDataContext'>
////    <local:DataGridLikeControl Name='target'>
////        <local:DataGridLikeControl.Columns>
////            <local:DataGridLikeColumn Binding='{CompiledBinding Length}' x:DataType='x:String'>
////                <local:DataGridLikeColumn.Template>
////                    <DataTemplate x:DataType='x:String'>
////                        <TextBlock Text='{CompiledBinding Length}' />
////                    </DataTemplate>
////                </local:DataGridLikeColumn.Template>
////            </local:DataGridLikeColumn>
////        </local:DataGridLikeControl.Columns>
////    </local:DataGridLikeControl>
////</Window>");
////                var target = window.GetControl<DataGridLikeControl>("target");
////                var column = target.Columns.Single();

////                var dataContext = new TestDataContext();
////                dataContext.ListProperty.Add("Test");
////                target.Items = dataContext.ListProperty;

////                window.ApplyTemplate();
////                target.ApplyTemplate();

////                // Assert DataGridLikeColumn.Binding data type.
////                var compiledPath = ((CompiledBindingExtension)column.Binding!).Path;
////                var node = Assert.IsType<PropertyElement>(Assert.Single(compiledPath.Elements));
////                Assert.Equal(typeof(int), node.Property.PropertyType);
                
////                // Assert DataGridLikeColumn.Template data type by evaluating the template.
////                var firstItem = dataContext.ListProperty[0];
////                var textBlockFromTemplate = (TextBlock)column.Template!.Build(firstItem)!;
////                textBlockFromTemplate.DataContext = firstItem;
////                Assert.Equal(firstItem.Length.ToString(), textBlockFromTemplate.Text);
////            }
////        }

        [Fact]
        public void ThrowsOnUninferrableDataTemplateInItemsControlWithoutItemsBinding()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <ItemsControl Name='target'>
        <ItemsControl.DataTemplates>
            <DataTemplate>
                <TextBlock Text='{CompiledBinding Property}' Name='textBlock' />
            </DataTemplate>
        </ItemsControl.DataTemplates>
    </ItemsControl>
</Window>";
                Assert.ThrowsAny<XmlException>(() => AvaloniaRuntimeXamlLoader.Load(xaml));
            }
        }
        
        [Fact]
        public void IgnoresDataTemplateTypeFromDataTypePropertyIfXDataTypeDefined()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Window.DataTemplates>
        <DataTemplate DataType='local:TestDataContextBaseClass' x:DataType='local:TestDataContext'>
            <TextBlock Text='{CompiledBinding StringProperty}' Name='textBlock' />
        </DataTemplate>
    </Window.DataTemplates>
    <ContentControl x:DataType='local:TestDataContext' Name='target' Content='{CompiledBinding}' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = window.GetControl<ContentControl>("target");

                var dataContext = new TestDataContext();

                dataContext.StringProperty = "Initial Value";

                window.DataContext = dataContext;

                window.ApplyTemplate();
                target.ApplyTemplate();
                target.Presenter!.UpdateChild();

                Assert.Equal(dataContext.StringProperty, ((TextBlock)target.Presenter.Child!).Text);
            }
        }
        
        [Fact]
        public void InfersCustomDataTemplateBasedOnAttribute()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Window.DataTemplates>
        <local:CustomDataTemplate FancyDataType='local:TestDataContext'>
            <TextBlock Text='{CompiledBinding StringProperty}' Name='textBlock' />
        </local:CustomDataTemplate>
    </Window.DataTemplates>
    <ContentControl x:DataType='local:TestDataContext' Name='target' Content='{CompiledBinding}' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = window.GetControl<ContentControl>("target");

                var dataContext = new TestDataContext();

                dataContext.StringProperty = "Initial Value";

                window.DataContext = dataContext;

                window.ApplyTemplate();
                target.ApplyTemplate();
                target.Presenter!.UpdateChild();

                Assert.Equal(dataContext.StringProperty, ((TextBlock)target.Presenter.Child!).Text);
            }
        }

        [Fact]
        public void InfersCustomDataTemplateBasedOnAttributeFromBaseClass()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Window.DataTemplates>
        <local:CustomDataTemplateInherit FancyDataType='local:TestDataContext'>
            <TextBlock Text='{CompiledBinding StringProperty}' Name='textBlock' />
        </local:CustomDataTemplateInherit>
    </Window.DataTemplates>
    <ContentControl x:DataType='local:TestDataContext' Name='target' Content='{CompiledBinding}' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = window.GetControl<ContentControl>("target");

                var dataContext = new TestDataContext();

                dataContext.StringProperty = "Initial Value";

                window.DataContext = dataContext;

                window.ApplyTemplate();
                target.ApplyTemplate();
                target.Presenter!.UpdateChild();

                Assert.Equal(dataContext.StringProperty, ((TextBlock)target.Presenter.Child!).Text);
            }
        }
        
        [Fact]
        public void ResolvesElementNameBinding()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <StackPanel>
        <TextBlock Text='{CompiledBinding StringProperty}' x:Name='text' />
        <TextBlock Text='{CompiledBinding #text.Text}' x:Name='text2' />
    </StackPanel>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("text2");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.StringProperty, textBlock.Text);
            }
        }

        [Fact]
        public void ResolvesElementNameBindingFromLongForm()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <StackPanel>
        <TextBlock Text='{CompiledBinding StringProperty}' x:Name='text' />
        <TextBlock Text='{CompiledBinding Text, ElementName=text}' x:Name='text2' />
    </StackPanel>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("text2");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.StringProperty, textBlock.Text);
            }
        }

        [Fact]
        public void ResolvesElementNameBindingFromLongFormWithoutPath()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <StackPanel>
        <TextBlock Text='{CompiledBinding StringProperty}' x:Name='text' />
        <TextBlock Text='{CompiledBinding ElementName=text}' x:Name='text2' />
    </StackPanel>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("text2");

                Assert.Equal("Avalonia.Controls.TextBlock", textBlock.Text);
            }
        }

        [Fact]
        public void ResolvesRelativeSourceBindingLongForm()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'
        Title='test'>
    <TextBlock Text='{CompiledBinding Title, RelativeSource={RelativeSource AncestorType=Window}}' x:Name='text'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = window.GetControl<TextBlock>("text");

                window.ApplyTemplate();
                window.Presenter!.ApplyTemplate();
                target.ApplyTemplate();

                Assert.Equal("test", target.Text);
            }
        }
        
        [Fact]
        public void ResolvesRelativeSourceBindingEvenLongerForm()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'
        Title='test'>
    <TextBlock Text='{CompiledBinding Title, RelativeSource={RelativeSource AncestorType={x:Type Window}}}' x:Name='text'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = window.GetControl<TextBlock>("text");

                window.ApplyTemplate();
                window.Presenter!.ApplyTemplate();
                target.ApplyTemplate();

                //Assert.Equal("test", target.Text);
            }
        }

        [Fact]
        public void ResolvesRelativeSourceBindingFromTemplate()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<ContentControl xmlns='https://github.com/avaloniaui'
                Focusable='True'>
    <ContentControl.Styles>
        <Style Selector='ContentControl'>
            <Setter Property='Template'>
                <ControlTemplate>
                    <ContentPresenter Focusable='{CompiledBinding !Focusable, RelativeSource={RelativeSource TemplatedParent}}' />
                </ControlTemplate>
            </Setter>
        </Style>
    </ContentControl.Styles>
</ContentControl>";

                var contentControl = AvaloniaRuntimeXamlLoader.Parse<ContentControl>(xaml);
                contentControl.DataContext = new TestDataContext(); // should be ignored
                contentControl.Measure(new Size(10, 10));
                
                var result = contentControl.GetTemplateChildren().OfType<ContentPresenter>().First();
                Assert.Equal(false, result.Focusable);
            }
        }
        
        [Fact]
        public void ResolvesRelativeSourceBindingFromStyleSelector()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<TextBox xmlns='https://github.com/avaloniaui'
         xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
         InnerLeftContent='Hello'>
    <TextBox.Styles>
        <Style Selector='TextBox'>
            <Setter Property='Template'>
                <ControlTemplate>
                    <StackPanel>
                        <ContentPresenter x:Name='Content' />
                        <TextPresenter x:Name='PART_TextPresenter' />
                    </StackPanel>
                </ControlTemplate>
            </Setter>
            <Style Selector='^ /template/ ContentPresenter#Content'>
                <Setter Property='Content' Value='{CompiledBinding InnerLeftContent, RelativeSource={RelativeSource TemplatedParent}}' />
            </Style>
        </Style>
    </TextBox.Styles>
</TextBox>";

                var textBox = AvaloniaRuntimeXamlLoader.Parse<TextBox>(xaml);
                textBox.DataContext = new TestDataContext(); // should be ignored
                textBox.Measure(new Size(10, 10));
                
                var result = textBox.GetTemplateChildren().OfType<ContentPresenter>().First();
                Assert.Equal(textBox.InnerLeftContent, result.Content);
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
              <ColumnDefinition Width='{CompiledBinding RelativeSource={RelativeSource TemplatedParent}, Path=Tag}'/>
            </Grid.ColumnDefinitions>
          </Grid>
        </ControlTemplate>
      </Button.Template>
    </Button>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var button = window.GetControl<Button>("button");

                button.Tag = new GridLength(5, GridUnitType.Star);

                window.ApplyTemplate();
                button.ApplyTemplate();

                Assert.Equal(button.Tag, button.GetTemplateChildren().OfType<Grid>().First().ColumnDefinitions[0].Width);
            }
        }

        [Fact]
        public void ResolvesElementNameInTemplate()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<ContentControl xmlns='https://github.com/avaloniaui'
                Content='Hello'>
    <ContentControl.Styles>
        <Style Selector='ContentControl'>
            <Setter Property='Template'>
                <ControlTemplate>
                    <Panel>
                        <TextBox Name='InnerTextBox' Text='Hello' />
                        <ContentPresenter Content='{CompiledBinding Text, ElementName=InnerTextBox}' />
                    </Panel>
                </ControlTemplate>
            </Setter>
        </Style>
    </ContentControl.Styles>
</ContentControl>";

                var contentControl = AvaloniaRuntimeXamlLoader.Parse<ContentControl>(xaml);
                contentControl.Measure(new Size(10, 10));
                
                var result = contentControl.GetTemplateChildren().OfType<ContentPresenter>().First();
                
                Assert.Equal("Hello", result.Content);
            }
        }
        
        [Fact]
        public void Binds_To_Source()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'
        Title='test'>
    <TextBlock Text='{CompiledBinding Length, Source=Test}' x:Name='text'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = window.GetControl<TextBlock>("text");

                window.ApplyTemplate();
                window.Presenter!.ApplyTemplate();
                target.ApplyTemplate();

                Assert.Equal("Test".Length.ToString(), target.Text);
            }
        }

        [Fact]
        public void Binds_To_Source_StaticResource()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='using:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions'
             x:CompileBindings='True'>
    <Window.Resources>
        <local:TestDataContext x:Key='dataKey' StringProperty='foobar'/>
    </Window.Resources>
    <TextBlock Name='textBlock' Text='{Binding StringProperty, Source={StaticResource dataKey}}'/>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                Assert.Equal("foobar", textBlock.Text);
            }
        }

        [Fact]
        public void Binds_To_Source_StaticResource1()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='using:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions'
             x:CompileBindings='True'>
    <Window.Resources>
        <local:TestDataContext x:Key='dataKey' StringProperty='foobar'/>
        <x:String x:Key='otherObjectKey'>test</x:String>
    </Window.Resources>
    <TextBlock Name='textBlock' Text='{Binding StringProperty, Source={StaticResource dataKey}}'/>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                Assert.Equal("foobar", textBlock.Text);
            }
        }

        [Fact]
        public void Binds_To_Source_StaticResource_In_ResourceDictionary()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='using:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions'
             x:DataType='local:TestDataContext' x:CompileBindings='True'>
    <Window.Resources>
        <ResourceDictionary>
            <local:TestDataContext x:Key='dataKey' StringProperty='foobar'/>
        </ResourceDictionary>
    </Window.Resources>
    <TextBlock Name='textBlock' Text='{Binding StringProperty, Source={StaticResource dataKey}}'/>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                Assert.Equal("foobar", textBlock.Text);
            }
        }

        [Fact]
        public void Binds_To_Source_StaticResource_In_ResourceDictionary1()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='using:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions'
             x:DataType='local:TestDataContext' x:CompileBindings='True'>
    <Window.Resources>
        <ResourceDictionary>
            <local:TestDataContext x:Key='dataKey' StringProperty='foobar'/>
            <x:String x:Key='otherObjectKey'>test</x:String>
        </ResourceDictionary>
    </Window.Resources>
    <TextBlock Name='textBlock' Text='{Binding StringProperty, Source={StaticResource dataKey}}'/>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                Assert.Equal("foobar", textBlock.Text);
            }
        }

        [Fact]
        public void Binds_To_Source_xStatic()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='using:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions'
             x:CompileBindings='True'>
    <ContentControl Name='contentControl' Content='{Binding Color, Source={x:Static Brushes.Red}}'/>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var contentControl = window.GetControl<ContentControl>("contentControl");

                Assert.Equal(Brushes.Red.Color, contentControl.Content);
            }
        }

        [Fact]
        public void CompilesBindingWhenRequested()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'
        x:CompileBindings='true'>
    <TextBlock Text='{Binding StringProperty}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.StringProperty, textBlock.Text);
            }
        }

        [Fact]
        public void ThrowsOnInvalidBindingPathOnCompiledBindingEnabledViaDirective()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'
        x:CompileBindings='true'>
    <TextBlock Text='{Binding InvalidPath}' Name='textBlock' />
</Window>";
                Assert.ThrowsAny<XmlException>(() => AvaloniaRuntimeXamlLoader.Load(xaml));
            }
        }

        [Fact]
        public void SupportParentInPath()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        Title='foo'>
    <ContentControl Content='{CompiledBinding $parent.Title}' Name='contentControl' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var contentControl = window.GetControl<ContentControl>("contentControl");

                Assert.Equal("foo", contentControl.Content);
            }
        }

        [Fact]
        public void SupportsParentInPathWithTypeAndLevelFilter()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border x:Name='p2'>
        <Border x:Name='p1'>
            <Button x:Name='p0'>
                <TextBlock x:Name='textBlock' Text='{CompiledBinding $parent[Control;1].Name}' />
            </Button>
        </Border>
    </Border>
</Window>");
                var textBlock = window.GetControl<TextBlock>("textBlock");

                Assert.Equal("p1", textBlock.Text);
            }
        }

        [Fact]
        public void SupportConverterWithParameter()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext' x:CompileBindings='True'>
    <TextBlock Name='textBlock' Text='{Binding StringProperty, Converter={x:Static local:AppendConverter.Instance}, ConverterParameter=Bar}'/>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                window.DataContext = new TestDataContext() { StringProperty = "Foo" };

                Assert.Equal($"Foo+Bar+{CultureInfo.CurrentCulture}", textBlock.Text);
            }
        }

        [Fact]
        public void SupportConverterWithCulture()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext' x:CompileBindings='True'>
    <TextBlock Name='textBlock' Text='{Binding StringProperty, Converter={x:Static local:AppendConverter.Instance}, ConverterCulture=ar-SA}'/>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                window.DataContext = new TestDataContext() { StringProperty = "Foo" };

                Assert.Equal($"Foo++ar-SA", textBlock.Text);
            }
        }

        [Fact]
        public void ThrowsOnInvalidCompileBindingsDirective()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'
        x:CompileBindings='notabool'>
</Window>";
                Assert.ThrowsAny<XmlException>(() => AvaloniaRuntimeXamlLoader.Load(xaml));
            }
        }

        [Fact]
        public void SupportCastToTypeInExpression()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='using:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions'
        x:DataType='local:TestDataContext'>
    <ContentControl Content='{CompiledBinding $parent.((local:TestDataContext)DataContext)}' Name='contentControl' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var contentControl = window.GetControl<ContentControl>("contentControl");

                var dataContext = new TestDataContext();

                window.DataContext = dataContext;

                Assert.Equal(dataContext, contentControl.Content);
            }
        }

        [Fact]
        public void SupportCastToTypeInExpression_DifferentTypeEvaluatesToNull()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='using:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions'
        x:DataType='local:TestDataContext'>
    <ContentControl Content='{CompiledBinding $parent.((local:TestDataContext)DataContext)}' Name='contentControl' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var contentControl = window.GetControl<ContentControl>("contentControl");

                var dataContext = "foo";

                window.DataContext = dataContext;

                Assert.Equal(null, contentControl.Content);
            }
        }

        [Fact]
        public void SupportCastToTypeInExpressionWithProperty()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='using:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions'
        x:DataType='local:TestDataContext'>
    <ContentControl Content='{CompiledBinding $parent.((local:TestDataContext)DataContext).StringProperty}' Name='contentControl' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var contentControl = window.GetControl<ContentControl>("contentControl");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.StringProperty, contentControl.Content);
            }
        }

        [Fact]
        public void SupportCastToTypeInExpressionWithProperty1()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='using:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions'
        x:DataType='local:TestDataContext'>
    <ContentControl Content='{CompiledBinding $parent.DataContext(local:TestDataContext).StringProperty}' Name='contentControl' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var contentControl = window.GetControl<ContentControl>("contentControl");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.StringProperty, contentControl.Content);
            }
        }

        [Fact]
        public void SupportCastToTypeInExpressionWithPropertyIndexer()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='using:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions'
        x:DataType='local:TestDataContext'>
    <ContentControl Content='{CompiledBinding ((local:TestData)ObjectsArrayProperty[0]).StringProperty}' Name='contentControl' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var contentControl = window.GetControl<ContentControl>("contentControl");

                var data = new TestData()
                {
                    StringProperty = "Foo"
                };
                var dataContext = new TestDataContext
                {
                    ObjectsArrayProperty = new object[] { data }
                };

                window.DataContext = dataContext;

                Assert.Equal(data.StringProperty, contentControl.Content);
            }
        }

        [Fact]
        public void SupportCastToTypeInExpressionWithProperty_DifferentTypeEvaluatesToNull()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='using:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions'
        x:DataType='local:TestDataContext'>
    <ContentControl Content='{CompiledBinding $parent.((local:TestDataContext)DataContext).StringProperty}' Name='contentControl' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var contentControl = window.GetControl<ContentControl>("contentControl");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.StringProperty, contentControl.Content);

                window.DataContext = "foo";

                Assert.Equal(null, contentControl.Content);
            }
        }

        [Fact]
        public void SupportsEmptyPath()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal(typeof(TestDataContext).FullName, textBlock.Text);
            }
        }

        [Fact]
        public void SupportsEmptyPathWithStringFormat()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding StringFormat=bar-\{0\}}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal("bar-" + typeof(TestDataContext).FullName, textBlock.Text);
            }
        }

        [Fact]
        public void SupportsDotPath()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding .}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal(typeof(TestDataContext).FullName, textBlock.Text);
            }
        }

        [Fact]
        public void SupportsExplicitDotPathWithStringFormat()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding Path=., StringFormat=bar-\{0\}}' Name='textBlock' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal("bar-" + typeof(TestDataContext).FullName, textBlock.Text);
            }
        }
        
        [Fact]
        public void SupportCastToTypeInExpressionWithProperty_ExplicitPropertyCast()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='using:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions'>
    <ContentControl Content='{CompiledBinding $parent.((local:IHasExplicitProperty)DataContext).ExplicitProperty}' Name='contentControl' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var contentControl = window.GetControl<ContentControl>("contentControl");

                var dataContext = new TestDataContext();

                window.DataContext = dataContext;

                Assert.Equal(((IHasExplicitProperty)dataContext).ExplicitProperty, contentControl.Content);
            }
        }

        [Fact]
        public void Binds_To_Self()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Name='textBlock' Text='{CompiledBinding $self}' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                window.ApplyTemplate();
                window.Presenter!.ApplyTemplate();

                Assert.Equal("Avalonia.Controls.TextBlock", textBlock.Text);
            }
        }

        [Fact]
        public void Binds_To_Self_Without_DataType()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <TextBlock Name='textBlock' Text='{CompiledBinding $self.Name}' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                window.ApplyTemplate();
                window.Presenter!.ApplyTemplate();

                Assert.Equal(textBlock.Name, textBlock.Text);
            }
        }

        [Fact]
        public void Binds_To_Self_In_Style()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
       
    <Window.Styles>
        <Style Selector='Button'>
            <Setter Property='IsVisible' Value='{CompiledBinding $self.IsEnabled}' />
        </Style>
    </Window.Styles>

    <Button Name='button' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var button = window.GetControl<Button>("button");

                window.ApplyTemplate();
                window.Presenter!.ApplyTemplate();

                Assert.True(button.IsVisible);

                button.IsEnabled = false;

                Assert.False(button.IsVisible);
            }
        }

        [Fact]
        public void Binds_To_RelativeSource_Self()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Name='textBlock' Text='{CompiledBinding RelativeSource={RelativeSource Self}}' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                window.ApplyTemplate();
                window.Presenter!.ApplyTemplate();

                Assert.Equal("Avalonia.Controls.TextBlock", textBlock.Text);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Binds_To_RelativeSource_Self_In_MultiBinding(bool compileBindings)
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = $@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        x:CompileBindings='{compileBindings}'>
  <StackPanel>
    <TextBlock Name='textBlock'>
      <TextBlock.Text>
        <MultiBinding StringFormat=""{{}} $self = {{0}}, $parent = {{1}}"">
          <Binding Path=""$self.FontStyle""/>
          <Binding Path=""$parent.Orientation""/>
        </MultiBinding>
      </TextBlock.Text>
    </TextBlock>
  </StackPanel>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext();
                window.DataContext = dataContext;

                Assert.Equal(" $self = Normal, $parent = Vertical"
                    , textBlock.GetValue(TextBlock.TextProperty));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Binds_To_RelativeSource_Self_In_MultiBinding_In_Style(bool compileBindings)
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = $@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        x:CompileBindings='{compileBindings}'>
  <Window.Styles>
    <Style Selector='TextBlock'>
        <Setter Property='Text'>
          <MultiBinding StringFormat=""{{}} $self = {{0}}"">
            <Binding Path=""$self.FontStyle""/>
          </MultiBinding>
        </Setter>
    </Style>
  </Window.Styles>
  <StackPanel>
    <TextBlock Name='textBlock'/>
  </StackPanel>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext();
                window.DataContext = dataContext;

                Assert.Equal(" $self = Normal"
                    , textBlock.GetValue(TextBlock.TextProperty));
            }
        }

        [Fact]
        public void SupportsMethodBindingAsDelegate()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:MethodDataContext'>
    <StackPanel>
        <ContentControl Content='{CompiledBinding Action}' Name='action' />
        <ContentControl Content='{CompiledBinding Func}' Name='func' />
        <ContentControl Content='{CompiledBinding Action16}' Name='action16' />
        <ContentControl Content='{CompiledBinding Func16}' Name='func16' />
        <ContentControl Content='{CompiledBinding CustomDelegateTypeVoid}' Name='customvoid' />
        <ContentControl Content='{CompiledBinding CustomDelegateTypeInt}' Name='customint' />
    </StackPanel>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                window.DataContext = new MethodDataContext();

                Assert.IsAssignableFrom(typeof(Action), window.GetControl<ContentControl>("action").Content);
                Assert.IsAssignableFrom(typeof(Func<int>), window.GetControl<ContentControl>("func").Content);
                Assert.IsAssignableFrom(typeof(Action<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>), window.GetControl<ContentControl>("action16").Content);
                Assert.IsAssignableFrom(typeof(Func<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>), window.GetControl<ContentControl>("func16").Content);
                Assert.True(typeof(Delegate).IsAssignableFrom(window.GetControl<ContentControl>("customvoid").Content!.GetType()));
                Assert.True(typeof(Delegate).IsAssignableFrom(window.GetControl<ContentControl>("customint").Content!.GetType()));
            }
        }

        [Fact]
        public void Binding_Method_To_Command_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:MethodAsCommandDataContext'>
    <Button Name='button' Command='{CompiledBinding Method}'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var button = window.GetControl<Button>("button");
                var vm = new MethodAsCommandDataContext();

                button.DataContext = vm;
                window.ApplyTemplate();

                Assert.NotNull(button.Command);
                PerformClick(button);
                Assert.Equal("Called", vm.Value);
            }
        }

        [Fact]
        public void Binding_Method_With_Parameter_To_Command_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:MethodAsCommandDataContext'>
    <Button Name='button' Command='{CompiledBinding Method1}' CommandParameter='5'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var button = window.GetControl<Button>("button");
                var vm = new MethodAsCommandDataContext();

                button.DataContext = vm;
                window.ApplyTemplate();

                Assert.NotNull(button.Command);
                PerformClick(button);
                Assert.Equal("Called 5", vm.Value);
            }
        }

        [Fact]
        public void Binding_Method_To_TextBlock_Text_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:MethodAsCommandDataContext'>
    <TextBlock Name='textBlock' Text='{CompiledBinding Method}'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");
                var vm = new MethodAsCommandDataContext();

                textBlock.DataContext = vm;
                window.ApplyTemplate();

                Assert.NotNull(textBlock.Text);
            }
        }


        [Theory]
        [InlineData(null, "Not called")]
        [InlineData("A", "Do A")]
        public void Binding_Method_With_Parameter_To_Command_CanExecute(object? commandParameter, string result)
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:MethodAsCommandDataContext'>
    <Button Name='button' Command='{CompiledBinding Do}' CommandParameter='{CompiledBinding Parameter, Mode=OneTime}'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var button = window.GetControl<Button>("button");
                var vm = new MethodAsCommandDataContext()
                {
                    Parameter = commandParameter
                };

                button.DataContext = vm;
                window.ApplyTemplate();

                Assert.NotNull(button.Command);
                PerformClick(button);
                Assert.Equal(vm.Value, result);
            }
        }

        [Fact]
        public void Binding_Method_With_Parameter_To_Command_CanExecute_DependsOn()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:MethodAsCommandDataContext'>
    <Button Name='button' Command='{CompiledBinding Do}'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var button = window.GetControl<Button>("button");
                var vm = new MethodAsCommandDataContext()
                {
                    Parameter = null,
                };

                button.DataContext = vm;
                window.ApplyTemplate();

                Assert.NotNull(button.Command);

                Assert.Equal(button.IsEffectivelyEnabled, false);

                vm.Parameter = true;
                Threading.Dispatcher.UIThread.RunJobs();

                Assert.Equal(button.IsEffectivelyEnabled, true);
            }
        }

        [Fact]
        public void Binding_Method_To_Command_In_Style_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:MethodAsCommandDataContext'>
    <Window.Styles>
        <Style Selector='Button'>
            <Setter Property='Command' Value='{CompiledBinding Method}'/>
        </Style>
    </Window.Styles>
    <Button Name='button'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var button = window.GetControl<Button>("button");
                var vm = new MethodAsCommandDataContext();

                button.DataContext = vm;
                window.ApplyTemplate();

                Assert.NotNull(button.Command);
                PerformClick(button);
                Assert.Equal("Called", vm.Value);
            }
        }

////        [Fact]
////        public void ResolvesDataTypeForAssignBinding()
////        {
////            using (UnitTestApplication.Start(TestServices.StyledWindow))
////            {
////                var xaml = @"
////<local:AssignBindingControl xmlns='https://github.com/avaloniaui'
////        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
////        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
////        x:DataType='local:TestDataContext'
////        X='{CompiledBinding StringProperty}' />";
////                var control = (AssignBindingControl)AvaloniaRuntimeXamlLoader.Load(xaml);
////                var compiledPath = ((CompiledBindingExtension)control.X!).Path;

////                var node = Assert.IsType<PropertyElement>(Assert.Single(compiledPath.Elements));
////                Assert.Equal(typeof(string), node.Property.PropertyType);
////            }
////        }
        
////        [Fact]
////        public void ResolvesDataTypeForAssignBinding_FromBindingProperty()
////        {
////            using (UnitTestApplication.Start(TestServices.StyledWindow))
////            {
////                var xaml = @"
////<local:AssignBindingControl xmlns='https://github.com/avaloniaui'
////        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
////        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
////        X='{CompiledBinding StringProperty, DataType=local:TestDataContext}' />";
////                var control = (AssignBindingControl)AvaloniaRuntimeXamlLoader.Load(xaml);
////                var compiledPath = ((CompiledBindingExtension)control.X!).Path;

////                var node = Assert.IsType<PropertyElement>(Assert.Single(compiledPath.Elements));
////                Assert.Equal(typeof(string), node.Property.PropertyType);
////            }
////        }
        
////        [Fact]
////        public void Uses_RuntimeLoader_Configuration_To_Enabled_Compiled()
////        {
////            using (UnitTestApplication.Start(TestServices.StyledWindow))
////            {
////                var xaml = @"
////<local:AssignBindingControl xmlns='https://github.com/avaloniaui'
////        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
////        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
////        X='{CompiledBinding StringProperty, DataType=local:TestDataContext}' />";
////                var control = (AssignBindingControl)AvaloniaRuntimeXamlLoader.Load(new RuntimeXamlLoaderDocument(xaml),
////                    new RuntimeXamlLoaderConfiguration { UseCompiledBindingsByDefault = true });
////                var compiledPath = ((CompiledBindingExtension)control.X!).Path;

////                var node = Assert.IsType<PropertyElement>(Assert.Single(compiledPath.Elements));
////                Assert.Equal(typeof(string), node.Property.PropertyType);
////            }
////        }
        
        [Fact]
        public void Should_Bind_To_Nested_Generic_Property()
        {
            // See https://github.com/AvaloniaUI/Avalonia/issues/10485
            // This code works fine with SRE, and test is passing, but it fails on Cecil.
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'
        x:CompileBindings='True'>
    <ComboBox x:Name='comboBox' ItemsSource='{Binding GenericProperty}' SelectedItem='{Binding GenericProperty.CurrentItem}' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var comboBox = window.GetControl<ComboBox>("comboBox");

                var dataContext = new TestDataContext();
                dataContext.GenericProperty.Add(123);
                dataContext.GenericProperty.CurrentItem = 123;
                window.DataContext = dataContext;

                Assert.Equal(123, comboBox.SelectedItem);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Should_Use_StringFormat_Without_Braces(bool compileBindings)
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = $@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'
        x:CompileBindings='{compileBindings}'>
    <TextBlock Name='textBlock' Text='{{Binding DecimalValue, StringFormat=c2}}'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext();
                window.DataContext = dataContext;

                Assert.Equal(string.Format("{0:c2}", TestDataContext.ExpectedDecimal)
                    , textBlock.GetValue(TextBlock.TextProperty));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Should_Negate_Boolean_Value(bool value)
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = $@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'
        x:CompileBindings='True'>
    <TextBlock Name='textBlock' Tag='{{Binding !BoolProperty}}'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext { BoolProperty = value };
                window.DataContext = dataContext;

                var result = Assert.IsType<bool>(textBlock.Tag);
                Assert.Equal(!value, result);
            }
        }

        [Fact]
        public void Can_Use_Implicit_Conversions()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = $@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:ImplicitConvertible'
        x:CompileBindings='True'>
    <TextBlock Name='textBlock'>
        <TextBlock.Background>
            <SolidColorBrush Color='{{Binding}}'/>
        </TextBlock.Background>
    </TextBlock>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.GetControl<TextBlock>("textBlock");

                var dataContext = new ImplicitConvertible("Green");
                window.DataContext = dataContext;

                var brush = Assert.IsType<SolidColorBrush>(textBlock.Background);
                Assert.Equal(Colors.Green, brush.Color);
            }
        }

        [Fact]
        public void Can_Bind_Brush_To_Hex_String()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = $@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestData'
        x:CompileBindings='True'>
    <TextBlock Name='textBlock' Background='{{Binding StringProperty}}'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                var dataContext = new TestData { StringProperty = "#ff0000" };
                window.DataContext = dataContext;

                var brush = Assert.IsType<ImmutableSolidColorBrush>(textBlock!.Background);
                Assert.Equal(Colors.Red, brush.Color);
            }
        }

        [Fact]
        public void ResolvesElementNameDataContextTypeBasedOnContext()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'
        x:Name='MyWindow'>
    <TextBlock Text='{CompiledBinding ElementName=MyWindow, Path=DataContext.StringProperty}' Name='textBlock' />
</Window>");
                var textBlock = window.GetControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.StringProperty, textBlock.Text);
            }
        }

        [Fact]
        public void ResolvesElementNameDataContextTypeBasedOnContextShortSyntax()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            { 
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'
        x:Name='MyWindow'>
    <TextBlock Text='{CompiledBinding #MyWindow.DataContext.StringProperty}' Name='textBlock' />
</Window>");
                var textBlock = window.GetControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.StringProperty, textBlock.Text);
            }
        }

        [Fact]
        public void TypeCastWorksWithElementNameDataContext()
        {
            // By default, DataContext will infer DataType from the XAML context, which will be local:TestDataContext here.
            // But developer should be able to re-define this type via type casing, if they know better.
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'
        x:Name='MyWindow'>
    <Panel>
        <TextBlock Text='{CompiledBinding $parent.((Button)DataContext).Tag}' Name='textBlock' />
    </Panel>
</Window>");
                var textBlock = window.GetControl<TextBlock>("textBlock");

                var panelDataContext = new Button { Tag = "foo" };
                ((Panel)window.Content!).DataContext = panelDataContext;

                Assert.Equal(panelDataContext.Tag, textBlock.Text);
            }
        }

        [Fact]
        public void ResolvesParentDataContextTypeBasedOnContext()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'
        x:Name='MyWindow'>
    <Panel>
        <TextBlock Text='{CompiledBinding $parent[Panel].DataContext.StringProperty}' Name='textBlock' />
    </Panel>
</Window>");
                var textBlock = window.GetControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.StringProperty, textBlock.Text);
            }
        }

        [Fact]
        public void ResolvesParentDataContextTypeBasedOnContextShortSyntax()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'
        x:Name='MyWindow'>
    <Panel>
        <TextBlock Text='{CompiledBinding $parent.DataContext.StringProperty}' Name='textBlock' />
    </Panel>
</Window>");
                var textBlock = window.GetControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.StringProperty, textBlock.Text);
            }
        }

        [Fact]
        public void Resolves_Nested_Generic_DataTypes()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='{x:Type local:TestDataContext+NestedGeneric, x:TypeArguments=x:String}'
        x:Name='MyWindow'>
    <Panel>
        <TextBlock Text='{CompiledBinding Value}' Name='textBlock' />
    </Panel>
</Window>");
                var textBlock = window.GetControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    NestedGenericString = new TestDataContext.NestedGeneric<string>
                    {
                        Value = "10"
                    }
                };

                window.DataContext = dataContext.NestedGenericString;

                Assert.Equal(dataContext.NestedGenericString.Value, textBlock.Text);
            }
        }

        static void Throws(string type, Action cb)
        {
            try
            {
                cb();
            }
            catch (Exception e) when (e.GetType().Name == type)
            {
                return;
            }

            throw new Exception("Expected " + type);
        }
        
        static void PerformClick(Button button)
        {
            button.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Enter,
            });
        }
    }
    
    public interface INonIntegerIndexer
    {
        string this[string key] { get; set; }
    }

    public interface INonIntegerIndexerDerived : INonIntegerIndexer
    { }

    public interface IHasProperty
    {
        string? StringProperty { get; set; }
    }

    public interface IHasPropertyDerived : IHasProperty
    { }

    public interface IHasExplicitProperty
    {
        string ExplicitProperty { get; }
    }

    public class AppendConverter : IValueConverter
    {
        public static IValueConverter Instance { get; } = new AppendConverter();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => string.Format("{0}+{1}+{2}", value, parameter, culture);

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();

    }

    public class TestData
    {
        public string? StringProperty { get; set; }
    }

    public class TestDataContextBaseClass {}
    
    public class TestItemsCollectionDataContext : TestDataContextBaseClass
    {
        public ObservableCollection<TestData> Items { get; } = new ObservableCollection<TestData>();
    }

    public class TestDataContext : TestDataContextBaseClass, IHasPropertyDerived, IHasExplicitProperty
    {
        public bool BoolProperty { get; set; }
        public string? StringProperty { get; set; }

        public Task<string>? TaskProperty { get; set; }

        public IObservable<string>? ObservableProperty { get; set; }

        public ObservableCollection<string> ObservableCollectionProperty { get; set; } = new ObservableCollection<string>();

        public string[]? ArrayProperty { get; set; }

        public object[]? ObjectsArrayProperty { get; set; }

        public List<string> ListProperty { get; set; } = new List<string>();

        public NonIntegerIndexer NonIntegerIndexerProperty { get; set; } = new NonIntegerIndexer();

        public INonIntegerIndexerDerived NonIntegerIndexerInterfaceProperty => NonIntegerIndexerProperty;

        public NestedGeneric<string>? NestedGenericString { get; init; }
        
        string IHasExplicitProperty.ExplicitProperty => "Hello"; 

        public string ExplicitProperty => "Bye";

        public static string StaticProperty => "World";

        public ListItemCollectionView<int> GenericProperty { get; } = new();

        public const decimal ExpectedDecimal = 15.756m;
        public decimal DecimalValue { get; set; } = ExpectedDecimal;

        public class NonIntegerIndexer : NotifyingBase, INonIntegerIndexerDerived
        {
            private readonly Dictionary<string, string> _storage = new Dictionary<string, string>();

            public string this[string key]
            {
                get => _storage[key];
                set
                {
                    _storage[key] = value;
                    RaisePropertyChanged(CommonPropertyNames.IndexerName);
                }
            }
        }

        public class NestedGeneric<T>
        {
            public T Value { get; set; }
        }
    }

    public class ListItemCollectionView<T> : List<T>
    {
        public T? CurrentItem { get; set; }
    }
    
    public class MethodDataContext
    {
        public void Action() { }

        public int Func() => 1;

        public void Action16(int i, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11, int i12, int i13, int i14, int i15, int i16) { }
        public int Func16(int i, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11, int i12, int i13, int i14, int i15, int i16) => i;
        public void CustomDelegateTypeVoid(int i, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11, int i12, int i13, int i14, int i15, int i16, int i17) { }
        public int CustomDelegateTypeInt(int i, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11, int i12, int i13, int i14, int i15, int i16, int i17) => i;
    }

    public class MethodAsCommandDataContext : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string Method() => Value = "Called";
        public string Method1(int i) => Value = $"Called {i}";
        public string Method2(int i, int j) => Value = $"Called {i},{j}";
        public string Value { get; private set; } = "Not called";

        private object? _parameter;

        public object? Parameter
        {
            get => _parameter;
            set
            {
                if (_parameter == value)
                {
                    return;
                }
                _parameter = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Parameter)));
            }
        }

        public void Do(object parameter)
        {
            Value = $"Do {parameter}";
        }

        [DependsOn(nameof(Parameter))]
        public bool CanDo(object parameter)
        {
            return ReferenceEquals(null, Parameter) == false;
        }
    }

    public class CustomDataTemplate : IDataTemplate
    {
        [DataType]
        public Type? FancyDataType { get; set; }

        [Content]
        [TemplateContent]
        public object? Content { get; set; }

        public bool Match(object? data) => FancyDataType?.IsInstanceOfType(data) ?? true;

        public Control? Build(object? data) => TemplateContent.Load(Content)?.Result;
    }
    
    public class CustomDataTemplateInherit : CustomDataTemplate { }

    public class AssignBindingControl : Control
    {
        [AssignBinding] public BindingBase? X { get; set; }
    }

    public class DataGridLikeControl : Control
    {
        public static readonly DirectProperty<DataGridLikeControl, IEnumerable?> ItemsProperty =
            AvaloniaProperty.RegisterDirect<DataGridLikeControl, IEnumerable?>(
                nameof(Items),
                x => x.Items,
                (x, v) => x.Items = v);

        private IEnumerable? _items;

        public IEnumerable? Items
        {
            get => _items;
            set => SetAndRaise(ItemsProperty, ref _items, value);
        }

        public AvaloniaList<DataGridLikeColumn> Columns { get; } = new();
    }

    public class DataGridLikeColumn
    {
        [AssignBinding]
        [InheritDataTypeFromItems(nameof(DataGridLikeControl.Items), AncestorType = typeof(DataGridLikeControl))]
        public BindingBase? Binding { get; set; }
        
        [InheritDataTypeFromItems(nameof(DataGridLikeControl.Items), AncestorType = typeof(DataGridLikeControl))]
        public IDataTemplate? Template { get; set; }
    }

    public class DataGridLikeControlInheritor : DataGridLikeControl
    { }

    public class ImplicitConvertible
    {
        public ImplicitConvertible(string value) => Value = value;
        
        public string Value { get; }
        
        public static implicit operator Avalonia.Media.Color(ImplicitConvertible value)
        {
            return Color.Parse(value.Value);
        }
    }
}
