using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class ControlTemplateTests : XamlTestBase
    {
        [Fact]
        public void StyledProperties_Should_Be_Set_In_The_ControlTemplate()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {

                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:controls=""using:Avalonia.Markup.Xaml.UnitTests.Xaml"">
    <Button>
        <Button.Template>
            <ControlTemplate>
                <controls:ListBoxHierarchyLine>
                    <controls:ListBoxHierarchyLine.LineDashStyle>
                        <DashStyle Dashes=""2,2"" Offset=""1"" />
                    </controls:ListBoxHierarchyLine.LineDashStyle>
                </controls:ListBoxHierarchyLine>
            </ControlTemplate>
        </Button.Template>
    </Button>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var button = (Button)window.Content;

                window.ApplyTemplate();
                button.ApplyTemplate();
                var listBoxHierarchyLine = button.GetVisualChildren().ElementAt(0) as ListBoxHierarchyLine;
                Assert.Equal(1, listBoxHierarchyLine.LineDashStyle.Offset);
                Assert.Equal(2, listBoxHierarchyLine.LineDashStyle.Dashes.Count);
                Assert.Equal(2, listBoxHierarchyLine.LineDashStyle.Dashes[0]);
                Assert.Equal(2, listBoxHierarchyLine.LineDashStyle.Dashes[1]);
            }
            
        }

        [Fact]
        public void Inline_ControlTemplate_Styled_Values_Are_Set_With_Style_Priority()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Button>
        <Button.Template>
            <ControlTemplate>
                <ContentPresenter Name='PART_ContentPresenter'
                                  Background='Red'/>
            </ControlTemplate>
        </Button.Template>
    </Button>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var button = (Button)window.Content;

                window.ApplyTemplate();
                button.ApplyTemplate();

                var presenter = button.Presenter;
                Assert.Equal(Brushes.Red, presenter.Background);

                var diagnostic = presenter.GetDiagnostic(Button.BackgroundProperty);
                Assert.Equal(BindingPriority.Template, diagnostic.Priority);
            }
        }

        [Fact]
        public void Style_ControlTemplate_Styled_Values_Are_Set_With_Style_Priority()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='Button'>
            <Setter Property='Template'>
                <ControlTemplate>
                    <ContentPresenter Name='PART_ContentPresenter'
                                      Background='Red'/>
                </ControlTemplate>
            </Setter>
        </Style>
    </Window.Styles>
    <Button/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var button = (Button)window.Content;

                window.ApplyTemplate();
                button.ApplyTemplate();

                var presenter = button.Presenter;
                Assert.Equal(Brushes.Red, presenter.Background);

                var diagnostic = presenter.GetDiagnostic(Button.BackgroundProperty);
                Assert.Equal(BindingPriority.Template, diagnostic.Priority);
            }
        }

        [Fact]
        public void ControlTemplate_Attached_Values_Are_Set_With_Style_Priority()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Button>
        <Button.Template>
            <ControlTemplate>
                <ContentPresenter Name='PART_ContentPresenter'
                                  DockPanel.Dock='Top'/>
            </ControlTemplate>
        </Button.Template>
    </Button>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var button = (Button)window.Content;

                window.ApplyTemplate();
                button.ApplyTemplate();

                var presenter = button.Presenter;
                Assert.Equal(Dock.Top, DockPanel.GetDock(presenter));

                var diagnostic = presenter.GetDiagnostic(DockPanel.DockProperty);
                Assert.Equal(BindingPriority.Template, diagnostic.Priority);
            }
        }

        [Fact]
        public void ControlTemplate_StaticResources_Are_Set_With_Style_Priority()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <SolidColorBrush x:Key='red'>Red</SolidColorBrush>
    </Window.Resources>
    <Button Content='Foo'>
        <Button.Template>
            <ControlTemplate>
                <ContentPresenter Name='PART_ContentPresenter'
                                  Background='{StaticResource red}'/>
            </ControlTemplate>
        </Button.Template>
    </Button>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var button = (Button)window.Content;

                window.ApplyTemplate();
                button.ApplyTemplate();

                var presenter = button.Presenter;
                Assert.Equal(Brushes.Red, presenter.Background);

                var diagnostic = presenter.GetDiagnostic(Button.BackgroundProperty);
                Assert.Equal(BindingPriority.Template, diagnostic.Priority);
            }
        }

        [Fact]
        public void ControlTemplate_DynamicResources_Are_Set_With_Style_Priority()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <SolidColorBrush x:Key='red'>Red</SolidColorBrush>
    </Window.Resources>
    <Button Content='Foo'>
        <Button.Template>
            <ControlTemplate>
                <ContentPresenter Name='PART_ContentPresenter'
                                  Background='{DynamicResource red}'/>
            </ControlTemplate>
        </Button.Template>
    </Button>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var button = (Button)window.Content;

                window.ApplyTemplate();
                button.ApplyTemplate();

                var presenter = button.Presenter;
                Assert.Equal(Brushes.Red, presenter.Background);

                var diagnostic = presenter.GetDiagnostic(Button.BackgroundProperty);
                Assert.Equal(BindingPriority.Template, diagnostic.Priority);
            }
        }

        [Fact]
        public void ControlTemplate_TemplateBindings_Are_Set_With_TemplatedParent_Priority()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Button Content='Foo'>
        <Button.Template>
            <ControlTemplate>
                <ContentPresenter Name='PART_ContentPresenter'
                                  Content='{TemplateBinding Content}'/>
            </ControlTemplate>
        </Button.Template>
    </Button>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var button = (Button)window.Content;

                window.ApplyTemplate();
                button.ApplyTemplate();

                var presenter = button.Presenter;
                Assert.Equal("Foo", presenter.Content);

                var diagnostic = presenter.GetDiagnostic(ContentPresenter.ContentProperty);
                Assert.Equal(BindingPriority.Template, diagnostic.Priority);
            }
        }

        [Fact]
        public void ControlTemplate_With_Nested_Child_Is_Operational()
        {
            var xaml = @"
<ControlTemplate xmlns='https://github.com/avaloniaui'>
    <ContentControl Name='parent'>
        <ContentControl Name='child' />
    </ContentControl>
</ControlTemplate>
";
            var template = AvaloniaRuntimeXamlLoader.Parse<ControlTemplate>(xaml);

            var parent = (ContentControl)template.Build(new ContentControl()).Result;

            Assert.Equal("parent", parent.Name);

            var child = parent.Content as ContentControl;

            Assert.NotNull(child);

            Assert.Equal("child", child.Name);
        }

        [Fact]
        public void ControlTemplate_With_TargetType_Is_Operational()
        {
            var xaml = @"
<ControlTemplate xmlns='https://github.com/avaloniaui' 
                 xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                 TargetType='{x:Type ContentControl}'>
    <ContentPresenter Content='{TemplateBinding Content}' />
</ControlTemplate>
";
            var template = AvaloniaRuntimeXamlLoader.Parse<ControlTemplate>(xaml);

            Assert.Equal(typeof(ContentControl), template.TargetType);

            Assert.IsType(typeof(ContentPresenter), template.Build(new ContentControl()).Result);
        }

        [Fact]
        public void ControlTemplate_With_String_TargetType()
        {
            var xaml = @"
<ControlTemplate xmlns='https://github.com/avaloniaui' 
                 xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                 TargetType='ContentControl'>
    <ContentPresenter Content='{TemplateBinding Content}' />
</ControlTemplate>
";
            var template = AvaloniaRuntimeXamlLoader.Parse<ControlTemplate>(xaml);

            Assert.Equal(typeof(ContentControl), template.TargetType);

            Assert.IsType(typeof(ContentPresenter), template.Build(new ContentControl()).Result);
        }


        [Fact]
        public void ControlTemplate_With_Panel_Children_Are_Added()
        {
            var xaml = @"
<ControlTemplate xmlns='https://github.com/avaloniaui'>
    <Panel Name='panel'>
        <ContentControl Name='Foo' />
        <ContentControl Name='Bar' />
    </Panel>
</ControlTemplate>
";
            var template = AvaloniaRuntimeXamlLoader.Parse<ControlTemplate>(xaml);

            var panel = (Panel)template.Build(new ContentControl()).Result;

            Assert.Equal(2, panel.Children.Count);

            var foo = panel.Children[0];
            var bar = panel.Children[1];

            Assert.Equal("Foo", foo.Name);
            Assert.Equal("Bar", bar.Name);
        }

        [Fact]
        public void ControlTemplate_Can_Be_Empty()
        {
            var xaml = "<ControlTemplate xmlns='https://github.com/avaloniaui' />";
            var template = AvaloniaRuntimeXamlLoader.Parse<ControlTemplate>(xaml);

            var templateResult = template.Build(new TemplatedControl());
            Assert.Null(templateResult);
        }
    }
    public class ListBoxHierarchyLine : Panel
    {
        public static readonly StyledProperty<DashStyle> LineDashStyleProperty =
        AvaloniaProperty.Register<ListBoxHierarchyLine, DashStyle>(nameof(LineDashStyle));

        public DashStyle LineDashStyle
        {
            get => GetValue(LineDashStyleProperty);
            set => SetValue(LineDashStyleProperty, value);
        }
    }
}
