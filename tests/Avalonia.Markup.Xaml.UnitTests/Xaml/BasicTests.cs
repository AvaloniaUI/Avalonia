// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Markup.Xaml.Data;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class BasicTests
    {
        [Fact]
        public void Simple_Property_Is_Set()
        {
            var xaml = @"<ContentControl xmlns='https://github.com/avaloniaui' Content='Foo'/>";

            var target = AvaloniaXamlLoader.Parse<ContentControl>(xaml);

            Assert.NotNull(target);
            Assert.Equal("Foo", target.Content);
        }

        [Fact]
        public void Default_Content_Property_Is_Set()
        {
            var xaml = @"<ContentControl xmlns='https://github.com/avaloniaui'>Foo</ContentControl>";

            var target = AvaloniaXamlLoader.Parse<ContentControl>(xaml);

            Assert.NotNull(target);
            Assert.Equal("Foo", target.Content);
        }

        [Fact]
        public void ContentControl_ContentTemplate_Is_Functional()
        {
            var xaml =
@"<ContentControl xmlns='https://github.com/avaloniaui'>
    <ContentControl.ContentTemplate>
        <DataTemplate>
            <TextBlock Text='Foo' />
        </DataTemplate>
    </ContentControl.ContentTemplate>
</ContentControl>";

            var contentControl = AvaloniaXamlLoader.Parse<ContentControl>(xaml);
            var target = contentControl.ContentTemplate;

            Assert.NotNull(target);

            var txt = (TextBlock)target.Build(null);

            Assert.Equal("Foo", txt.Text);
        }

        [Fact]
        public void Named_Control_Is_Added_To_NameScope_Simple()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'>
    <Button Name='button'>Foo</Button>
</UserControl>";

            var control = AvaloniaXamlLoader.Parse<UserControl>(xaml);
            var button = control.FindControl<Button>("button");

            Assert.Equal("Foo", button.Content);
        }

        [Fact]
        public void Named_x_Control_Is_Added_To_NameScope_Simple()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Button x:Name='button'>Foo</Button>
</UserControl>";

            var control = AvaloniaXamlLoader.Parse<UserControl>(xaml);
            var button = control.FindControl<Button>("button");

            Assert.Equal("Foo", button.Content);
        }

        [Fact]
        public void Standart_TypeConverter_Is_Used()
        {
            var xaml = @"<UserControl xmlns='https://github.com/avaloniaui' Width='200.5' />";

            var control = AvaloniaXamlLoader.Parse<UserControl>(xaml);
            Assert.Equal(200.5, control.Width);
        }

        [Fact]
        public void Avalonia_TypeConverter_Is_Used()
        {
            var xaml = @"<UserControl xmlns='https://github.com/avaloniaui' Background='White' />";

            var control = AvaloniaXamlLoader.Parse<UserControl>(xaml);
            var bk = control.Background;
            Assert.IsType<SolidColorBrush>(bk);
            Assert.Equal(Colors.White, (bk as SolidColorBrush).Color);
        }

        [Fact]
        public void Simple_Style_Is_Parsed()
        {
            var xaml = @"
<Styles xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Style Selector='TextBlock'>
        <Setter Property='Background' Value='White'/>
        <Setter Property='Width' Value='100'/>
    </Style>
</Styles>";

            var styles = AvaloniaXamlLoader.Parse<Styles>(xaml);

            Assert.Equal(1, styles.Count);

            var style = (Style)styles[0];

            var setters = style.Setters.Cast<Setter>().ToArray();

            Assert.Equal(2, setters.Length);

            Assert.Equal(TextBlock.BackgroundProperty, setters[0].Property);
            Assert.Equal(Brushes.White, setters[0].Value);

            Assert.Equal(TextBlock.WidthProperty, setters[1].Property);
            Assert.Equal(100.0, setters[1].Value);
        }

        [Fact]
        public void Style_Resources_Are_Build()
        {
            var xaml = @"
<Style xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:sys='clr-namespace:System;assembly=mscorlib'>
    <Style.Resources>
        <SolidColorBrush x:Key='Brush'>White</SolidColorBrush>
        <sys:Double x:Key='Double'>10</sys:Double>
    </Style.Resources>
</Style>";

            var style = AvaloniaXamlLoader.Parse<Style>(xaml);

            Assert.True(style.Resources.Count > 0);

            var brush = style.FindResource("Brush") as SolidColorBrush;

            Assert.NotNull(brush);

            Assert.Equal(Colors.White, brush.Color);

            var d = (double)style.FindResource("Double");

            Assert.Equal(10.0, d);
        }

        [Fact]
        public void StyleInclude_Is_Build()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Styles xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <StyleInclude Source='resm:Avalonia.Themes.Default.Accents.BaseLight.xaml?assembly=Avalonia.Themes.Default'/>
</Styles>";

                var styles = AvaloniaXamlLoader.Parse<Styles>(xaml);

                Assert.True(styles.Count == 1);

                var styleInclude = styles.First() as StyleInclude;

                Assert.NotNull(styleInclude);

                var style = styleInclude.Loaded;

                Assert.NotNull(style);
            }
        }

        [Fact]
        public void Simple_Xaml_Binding_Is_Operational()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformWrapper
                                    .With(windowingPlatform: new MockWindowingPlatform())))
            {
                var xaml =
@"<Window xmlns='https://github.com/avaloniaui' Content='{Binding}'/>";

                var target = AvaloniaXamlLoader.Parse<ContentControl>(xaml);

                Assert.Null(target.Content);

                target.DataContext = "Foo";

                Assert.Equal("Foo", target.Content);
            }
        }

        [Fact]
        public void Control_Template_Is_Operational()
        {
            var xaml = @"
<ContentControl xmlns='https://github.com/avaloniaui' 
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ContentControl.Template>
        <ControlTemplate>
            <ContentPresenter Name='PART_ContentPresenter' 
                        Content='{TemplateBinding Content}'/>
        </ControlTemplate>
    </ContentControl.Template>
</ContentControl>";

            var target = AvaloniaXamlLoader.Parse<ContentControl>(xaml);

            Assert.NotNull(target.Template);

            Assert.Null(target.Presenter);

            target.ApplyTemplate();

            Assert.NotNull(target.Presenter);

            target.Content = "Foo";

            Assert.Equal("Foo", target.Presenter.Content);
        }

        [Fact]
        public void Style_ControlTemplate_Is_Build()
        {
                var xaml = @"
<Style xmlns='https://github.com/avaloniaui' Selector='ContentControl'>
  <Setter Property='Template'>
     <ControlTemplate>
        <ContentPresenter Name='PART_ContentPresenter'
                       Content='{TemplateBinding Content}'
                       ContentTemplate='{TemplateBinding ContentTemplate}' />
      </ControlTemplate>
  </Setter>
</Style> ";

                var style = AvaloniaXamlLoader.Parse<Style>(xaml);

                Assert.Equal(1, style.Setters.Count());

                var setter = (Setter)style.Setters.First();

                Assert.Equal(ContentControl.TemplateProperty, setter.Property);

                Assert.IsType<ControlTemplate>(setter.Value);

                var template = (ControlTemplate)setter.Value;

                var control = new ContentControl();

                var result = (ContentPresenter)template.Build(control);

                Assert.NotNull(result);
        }

        [Fact]
        public void Named_Control_Is_Added_To_NameScope()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Button Name='button'>Foo</Button>
</Window>";

                var window = AvaloniaXamlLoader.Parse<Window>(xaml);
                var button = window.FindControl<Button>("button");

                Assert.Equal("Foo", button.Content);
            }
        }

        [Fact]
        public void Control_Is_Added_To_Parent_Before_Properties_Are_Set()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <local:InitializationOrderTracker Width='100'/>
</Window>";

                var window = AvaloniaXamlLoader.Parse<Window>(xaml);
                var tracker = (InitializationOrderTracker)window.Content;

                var attached = tracker.Order.IndexOf("AttachedToLogicalTree");
                var widthChanged = tracker.Order.IndexOf("Property Width Changed");

                Assert.NotEqual(-1, attached);
                Assert.NotEqual(-1, widthChanged);
                Assert.True(attached < widthChanged);
            }
        }
    }
}