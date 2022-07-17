﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
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
using Avalonia.Metadata;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.MarkupExtensions
{
    public class CompiledBindingExtensionTests
    {
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
                var textBlock = window.FindControl<TextBlock>("textBlock");

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
                var textBlock = window.FindControl<TextBlock>("textBlock");

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
                var textBlock = window.FindControl<TextBlock>("textBlock");

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
    <ItemsControl Name='itemsControl' Items='{CompiledBinding Path=ListProperty}'>
	    <ItemsControl.ItemTemplate>
		    <DataTemplate>
			    <TextBlock />
		    </DataTemplate>
	    </ItemsControl.ItemTemplate>
    </ItemsControl>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<ItemsControl>("itemsControl");

                var dataContext = new TestDataContext
                {
                    ListProperty =
                    {
                        "Hello"
                    } 
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.ListProperty, textBlock.Items);
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
                var textBlock = window.FindControl<TextBlock>("textBlock");

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
                var textBlock = window.FindControl<TextBlock>("textBlock");

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
                var textBlock = window.FindControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    TaskProperty = Task.FromResult("foobar")
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.TaskProperty.Result, textBlock.Text);
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
                var textBlock = window.FindControl<TextBlock>("textBlock");

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
                var textBlock = window.FindControl<TextBlock>("textBlock");

                var dataContext = new TestDataContext
                {
                    ListProperty = { "A", "B", "C", "D", "E" }
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.ListProperty[3], textBlock.Text);
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
                var textBlock = window.FindControl<TextBlock>("textBlock");

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
                var textBlock = window.FindControl<TextBlock>("textBlock");

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
                var textBlock = window.FindControl<TextBlock>("textBlock");

                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();

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
                var textBlock = window.FindControl<TextBlock>("textBlock");

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
                var textBlock = window.FindControl<TextBlock>("textBlock");

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
                var target = window.FindControl<ContentControl>("target");

                var dataContext = new TestDataContext();

                dataContext.StringProperty = "Initial Value";

                window.DataContext = dataContext;

                window.ApplyTemplate();
                target.ApplyTemplate();
                ((ContentPresenter)target.Presenter).UpdateChild();

                Assert.Equal(dataContext.StringProperty, ((TextBlock)target.Presenter.Child).Text);
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
                ThrowsXamlTransformException(() => AvaloniaRuntimeXamlLoader.Load(xaml));
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
                ThrowsXamlTransformException(() => AvaloniaRuntimeXamlLoader.Load(xaml));
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
    <ItemsControl Items='{CompiledBinding ListProperty}' Name='target'>
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <TextBlock Text='{CompiledBinding}' Name='textBlock' />
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = window.FindControl<ItemsControl>("target");

                var dataContext = new TestDataContext();

                dataContext.ListProperty.Add("Test");

                window.DataContext = dataContext;

                window.ApplyTemplate();
                target.ApplyTemplate();
                target.Presenter.ApplyTemplate();

                Assert.Equal(dataContext.ListProperty[0], (string)((ContentPresenter)target.Presenter.Panel.Children[0]).Content);
            }
        }

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
                ThrowsXamlTransformException(() => AvaloniaRuntimeXamlLoader.Load(xaml));
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
                var target = window.FindControl<ContentControl>("target");

                var dataContext = new TestDataContext();

                dataContext.StringProperty = "Initial Value";

                window.DataContext = dataContext;

                window.ApplyTemplate();
                target.ApplyTemplate();
                ((ContentPresenter)target.Presenter).UpdateChild();

                Assert.Equal(dataContext.StringProperty, ((TextBlock)target.Presenter.Child).Text);
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
                var target = window.FindControl<ContentControl>("target");

                var dataContext = new TestDataContext();

                dataContext.StringProperty = "Initial Value";

                window.DataContext = dataContext;

                window.ApplyTemplate();
                target.ApplyTemplate();
                ((ContentPresenter)target.Presenter).UpdateChild();

                Assert.Equal(dataContext.StringProperty, ((TextBlock)target.Presenter.Child).Text);
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
                var target = window.FindControl<ContentControl>("target");

                var dataContext = new TestDataContext();

                dataContext.StringProperty = "Initial Value";

                window.DataContext = dataContext;

                window.ApplyTemplate();
                target.ApplyTemplate();
                ((ContentPresenter)target.Presenter).UpdateChild();

                Assert.Equal(dataContext.StringProperty, ((TextBlock)target.Presenter.Child).Text);
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
                var textBlock = window.FindControl<TextBlock>("text2");

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
                var textBlock = window.FindControl<TextBlock>("text2");

                var dataContext = new TestDataContext
                {
                    StringProperty = "foobar"
                };

                window.DataContext = dataContext;

                Assert.Equal(dataContext.StringProperty, textBlock.Text);
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
                var target = window.FindControl<TextBlock>("text");

                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();
                target.ApplyTemplate();

                Assert.Equal("test", target.Text);
            }
        }

        [Fact]
        public void ResolvesRelativeSourceBindingFromTemplate()
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
                    <ContentPresenter Content='{CompiledBinding Content, RelativeSource={RelativeSource TemplatedParent}}' />
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
                var target = window.FindControl<TextBlock>("text");

                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();
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
                var textBlock = window.FindControl<TextBlock>("textBlock");

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
                var textBlock = window.FindControl<TextBlock>("textBlock");

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
                var textBlock = window.FindControl<TextBlock>("textBlock");

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
                var textBlock = window.FindControl<TextBlock>("textBlock");

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
                var contentControl = window.FindControl<ContentControl>("contentControl");

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
                var textBlock = window.FindControl<TextBlock>("textBlock");

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
                ThrowsXamlParseException(() => AvaloniaRuntimeXamlLoader.Load(xaml));
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
                var contentControl = window.FindControl<ContentControl>("contentControl");

                Assert.Equal("foo", contentControl.Content);
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
                var textBlock = window.FindControl<TextBlock>("textBlock");

                window.DataContext = new TestDataContext() { StringProperty = "Foo" };

                Assert.Equal("Foo+Bar", textBlock.Text);
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
                ThrowsXamlParseException(() => AvaloniaRuntimeXamlLoader.Load(xaml));
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
                var contentControl = window.FindControl<ContentControl>("contentControl");

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
                var contentControl = window.FindControl<ContentControl>("contentControl");

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
                var contentControl = window.FindControl<ContentControl>("contentControl");

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
                var contentControl = window.FindControl<ContentControl>("contentControl");

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
                var contentControl = window.FindControl<ContentControl>("contentControl");

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
                var contentControl = window.FindControl<ContentControl>("contentControl");

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
                var textBlock = window.FindControl<TextBlock>("textBlock");

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
                var textBlock = window.FindControl<TextBlock>("textBlock");

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
                var textBlock = window.FindControl<TextBlock>("textBlock");

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
                var textBlock = window.FindControl<TextBlock>("textBlock");

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
                var textBlock = window.FindControl<TextBlock>("textBlock");

                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();

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
                var button = window.FindControl<Button>("button");

                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();

                Assert.True(button.IsVisible);

                button.IsEnabled = false;

                Assert.False(button.IsVisible);
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

                Assert.IsAssignableFrom(typeof(Action), window.FindControl<ContentControl>("action").Content);
                Assert.IsAssignableFrom(typeof(Func<int>), window.FindControl<ContentControl>("func").Content);
                Assert.IsAssignableFrom(typeof(Action<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>), window.FindControl<ContentControl>("action16").Content);
                Assert.IsAssignableFrom(typeof(Func<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>), window.FindControl<ContentControl>("func16").Content);
                Assert.True(typeof(Delegate).IsAssignableFrom(window.FindControl<ContentControl>("customvoid").Content.GetType()));
                Assert.True(typeof(Delegate).IsAssignableFrom(window.FindControl<ContentControl>("customint").Content.GetType()));
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
                var button = window.FindControl<Button>("button");
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
                var button = window.FindControl<Button>("button");
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
                var textBlock = window.FindControl<TextBlock>("textBlock");
                var vm = new MethodAsCommandDataContext();

                textBlock.DataContext = vm;
                window.ApplyTemplate();

                Assert.NotNull(textBlock.Text);
            }
        }


        [Theory]
        [InlineData(null, "Not called")]
        [InlineData("A", "Do A")]
        public void Binding_Method_With_Parameter_To_Command_CanExecute(object commandParameter, string result)
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
                var button = window.FindControl<Button>("button");
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
    <Button Name='button' Command='{CompiledBinding Do}' CommandParameter='{CompiledBinding Parameter, Mode=OneWay}'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var button = window.FindControl<Button>("button");
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
        public void ResolvesDataTypeForAssignBinding()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<local:AssignBindingControl xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        x:DataType='local:TestDataContext'
        X='{CompiledBinding StringProperty}' />";
                var control = (AssignBindingControl)AvaloniaRuntimeXamlLoader.Load(xaml);
                var compiledPath = ((CompiledBindingExtension)control.X).Path;

                var node = Assert.IsType<PropertyElement>(Assert.Single(compiledPath.Elements));
                Assert.Equal(typeof(string), node.Property.PropertyType);
            }
        }
        
        [Fact]
        public void ResolvesDataTypeForAssignBinding_FromBindingProperty()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<local:AssignBindingControl xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        X='{CompiledBinding StringProperty, DataType=local:TestDataContext}' />";
                var control = (AssignBindingControl)AvaloniaRuntimeXamlLoader.Load(xaml);
                var compiledPath = ((CompiledBindingExtension)control.X).Path;

                var node = Assert.IsType<PropertyElement>(Assert.Single(compiledPath.Elements));
                Assert.Equal(typeof(string), node.Property.PropertyType);
            }
        }
        
        void Throws(string type, Action cb)
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

        void ThrowsXamlParseException(Action cb) => Throws("XamlParseException", cb);
        void ThrowsXamlTransformException(Action cb) => Throws("XamlTransformException", cb);


        static void PerformClick(Button button)
        {
            button.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Input.Key.Enter,
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
        string StringProperty { get; set; }
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

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => string.Format("{0}+{1}", value, parameter);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

    }

    public class TestData
    {
        public string StringProperty { get; set; }
    }

    public class TestDataContextBaseClass {}
    
    public class TestDataContext : TestDataContextBaseClass, IHasPropertyDerived, IHasExplicitProperty
    {
        public string StringProperty { get; set; }

        public Task<string> TaskProperty { get; set; }

        public IObservable<string> ObservableProperty { get; set; }

        public ObservableCollection<string> ObservableCollectionProperty { get; set; } = new ObservableCollection<string>();

        public string[] ArrayProperty { get; set; }

        public object[] ObjectsArrayProperty { get; set; }

        public List<string> ListProperty { get; set; } = new List<string>();

        public NonIntegerIndexer NonIntegerIndexerProperty { get; set; } = new NonIntegerIndexer();

        public INonIntegerIndexerDerived NonIntegerIndexerInterfaceProperty => NonIntegerIndexerProperty;

        string IHasExplicitProperty.ExplicitProperty => "Hello"; 

        public string ExplicitProperty => "Bye"; 

        public class NonIntegerIndexer : NotifyingBase, INonIntegerIndexerDerived
        {
            private readonly Dictionary<string, string> _storage = new Dictionary<string, string>();

            public string this[string key]
            {
                get
                {
                    return _storage[key];
                }
                set
                {
                    _storage[key] = value;
                    RaisePropertyChanged(CommonPropertyNames.IndexerName);
                }
            }
        }
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
        public event PropertyChangedEventHandler PropertyChanged;

        public string Method() => Value = "Called";
        public string Method1(int i) => Value = $"Called {i}";
        public string Method2(int i, int j) => Value = $"Called {i},{j}";
        public string Value { get; private set; } = "Not called";

        object _parameter;
        public object Parameter
        {
            get
            {
                return _parameter;
            }
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

        [Metadata.DependsOn(nameof(Parameter))]
        public bool CanDo(object parameter)
        {
            return ReferenceEquals(null, parameter) == false;
        }
    }

    public class CustomDataTemplate : IDataTemplate
    {
        [DataType]
        public Type FancyDataType { get; set; }

        [Content]
        [TemplateContent]
        public object Content { get; set; }

        public bool Match(object data) => FancyDataType?.IsInstanceOfType(data) ?? true;

        public IControl Build(object data) => TemplateContent.Load(Content)?.Control;
    }
    
    public class CustomDataTemplateInherit : CustomDataTemplate { }

    public class AssignBindingControl : Control
    {
        [AssignBinding] public IBinding X { get; set; }
    }
}
