using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Metadata;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class SampleTemplatedObject : StyledElement
    {
        [Content] public List<SampleTemplatedObject> Content { get; set; } = new List<SampleTemplatedObject>();
        public string Foo { get; set; }
    }
    
    public class SampleTemplatedObjectTemplate
    {
        [Content]
        [TemplateContent(TemplateResultType = typeof(SampleTemplatedObject))]
        public object Content { get; set; }
    }

    public class SampleTemplatedObjectContainer
    {
        public SampleTemplatedObjectTemplate Template { get; set; }
    }
    
    public class GenericTemplateTests
    {
        [Fact]
        public void DataTemplate_Can_Be_Empty()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<s:SampleTemplatedObjectContainer xmlns='https://github.com/avaloniaui'
        xmlns:sys='clr-namespace:System;assembly=netstandard'
        xmlns:s='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <s:SampleTemplatedObjectContainer.Template>
        <s:SampleTemplatedObjectTemplate>
            <s:SampleTemplatedObject x:Name='root'>
                <s:SampleTemplatedObject x:Name='child1' Foo='foo' />
                <s:SampleTemplatedObject x:Name='child2' Foo='bar' />
            </s:SampleTemplatedObject>
        </s:SampleTemplatedObjectTemplate>
    </s:SampleTemplatedObjectContainer.Template>
</s:SampleTemplatedObjectContainer>";
                var container =
                    (SampleTemplatedObjectContainer)AvaloniaRuntimeXamlLoader.Load(xaml,
                        typeof(GenericTemplateTests).Assembly);
                var res = TemplateContent.Load<SampleTemplatedObject>(container.Template.Content);
                Assert.Equal(res.Result, res.NameScope.Find("root"));
                Assert.Equal(res.Result.Content[0], res.NameScope.Find("child1"));
                Assert.Equal(res.Result.Content[1], res.NameScope.Find("child2"));
                Assert.Equal("foo", res.Result.Content[0].Foo);
                Assert.Equal("bar", res.Result.Content[1].Foo);
            }
        }
    }
}
