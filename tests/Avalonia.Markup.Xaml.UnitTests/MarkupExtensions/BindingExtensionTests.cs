using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.MarkupExtensions
{
    public class BindingExtensionTests : XamlTestBase
    {

        [Fact]
        public void BindingExtension_Binds_To_Source()
        {
            using (StyledWindow())
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <x:String x:Key='text'>foobar</x:String>
    </Window.Resources>

    <TextBlock Name='textBlock' Text='{Binding Source={StaticResource text}}'/>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                window.Show();

                Assert.Equal("foobar", textBlock.Text);
            }
        }

        [Fact]
        public void BindingExtension_Binds_To_TargetNullValue()
        {
            using (StyledWindow())
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <x:String x:Key='text'>foobar</x:String>
    </Window.Resources>

    <TextBlock Name='textBlock' Text='{Binding Foo, TargetNullValue={StaticResource text}}'/>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                window.DataContext = new FooBar();
                window.Show();

                Assert.Equal("foobar", textBlock.Text);
            }
        }

        [Fact]
        public void BindingExtension_TargetNullValue_UnsetByDefault()
        {
            using (StyledWindow())
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock Name='textBlock' IsVisible='{Binding Foo, Converter={x:Static ObjectConverters.IsNotNull}}'/>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                window.DataContext = new FooBar();
                window.Show();

                Assert.Equal(false, textBlock.IsVisible);
            }
        }

        private class FooBar
        {
            public object Foo { get; } = null;
        }

        private IDisposable StyledWindow(params (string, string)[] assets)
        {
            var services = TestServices.StyledWindow.With(
                assetLoader: new MockAssetLoader(assets),
                theme: () => new Styles
                {
                    WindowStyle(),
                });

            return UnitTestApplication.Start(services);
        }

        private Style WindowStyle()
        {
            return new Style(x => x.OfType<Window>())
            {
                Setters =
                {
                    new Setter(
                        Window.TemplateProperty,
                        new FuncControlTemplate<Window>((x, scope) =>
                            new VisualLayerManager
                            {
                                Child =
                                    new ContentPresenter
                                    {
                                        Name = "PART_ContentPresenter",
                                        [!ContentPresenter.ContentProperty] = x[!Window.ContentProperty],
                                    }.RegisterInNameScope(scope)
                            }))
                }
            };
        }
    }
}
