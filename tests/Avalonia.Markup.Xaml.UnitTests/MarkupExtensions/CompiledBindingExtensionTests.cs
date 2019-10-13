using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data.Core;
using Avalonia.Markup.Data;
using Avalonia.UnitTests;
using XamlIl;
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
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
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
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
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
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
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
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
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
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
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
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
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
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
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
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
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
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
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
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
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
                var loader = new AvaloniaXamlLoader();
                Assert.Throws<XamlIlTransformException>(() => loader.Load(xaml));
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
                var loader = new AvaloniaXamlLoader();
                Assert.Throws<XamlIlTransformException>(() => loader.Load(xaml));
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
        <ItemsControl.DataTemplates>
            <DataTemplate>
                <TextBlock Text='{CompiledBinding}' Name='textBlock' />
            </DataTemplate>
        </ItemsControl.DataTemplates>
    </ItemsControl>
</Window>";
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
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
                var loader = new AvaloniaXamlLoader();
                Assert.Throws<XamlIlTransformException>(() => loader.Load(xaml));
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
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
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
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
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
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var target = window.FindControl<TextBlock>("text");

                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();
                target.ApplyTemplate();

                Assert.Equal("test", target.Text);
            }
        }
    }

    public class TestDataContext
    {
        public string StringProperty { get; set; }

        public Task<string> TaskProperty { get; set; }

        public IObservable<string> ObservableProperty { get; set; }

        public ObservableCollection<string> ObservableCollectionProperty { get; set; } = new ObservableCollection<string>();

        public string[] ArrayProperty { get; set; }

        public List<string> ListProperty { get; set; } = new List<string>();

        public NonIntegerIndexer NonIntegerIndexerProperty { get; set; } = new NonIntegerIndexer();

        public class NonIntegerIndexer : NotifyingBase
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
}
