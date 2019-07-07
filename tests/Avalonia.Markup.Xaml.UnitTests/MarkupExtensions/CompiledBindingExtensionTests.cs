using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Data;
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
        x:DataContextType='{x:Type local:TestDataContext}'>
    <TextBlock Text='{CompiledBinding StringProperty}' Name='textBlock' />
</Window>";
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                DelayedBinding.ApplyBindings(textBlock);

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
        x:DataContextType='{x:Type local:TestDataContext}'>
    <TextBlock Text='{CompiledBinding TaskProperty^}' Name='textBlock' />
</Window>";
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                DelayedBinding.ApplyBindings(textBlock);

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
        x:DataContextType='{x:Type local:TestDataContext}'>
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
    }

    public class TestDataContext
    {
        public string StringProperty { get; set; }

        public Task<string> TaskProperty { get; set; }

        public IObservable<string> ObservableProperty { get; set; }
    }
}
