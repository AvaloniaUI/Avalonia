using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Metadata;
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
                var button = (Button)window.Content!;

                window.ApplyTemplate();
                button.ApplyTemplate();
                var listBoxHierarchyLine = button.GetVisualChildren().ElementAt(0) as ListBoxHierarchyLine;
                Assert.NotNull(listBoxHierarchyLine);
                Assert.NotNull(listBoxHierarchyLine.LineDashStyle);
                Assert.NotNull(listBoxHierarchyLine.LineDashStyle.Dashes);
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
                var button = (Button)window.Content!;

                window.ApplyTemplate();
                button.ApplyTemplate();

                var presenter = button.Presenter!;
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
                var button = (Button)window.Content!;

                window.ApplyTemplate();
                button.ApplyTemplate();

                var presenter = button.Presenter;
                Assert.NotNull(presenter);
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
                var button = (Button)window.Content!;

                window.ApplyTemplate();
                button.ApplyTemplate();

                var presenter = button.Presenter;
                Assert.NotNull(presenter);
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
                var button = (Button)window.Content!;

                window.ApplyTemplate();
                button.ApplyTemplate();

                var presenter = button.Presenter;
                Assert.NotNull(presenter);
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
                var button = (Button)window.Content!;

                window.ApplyTemplate();
                button.ApplyTemplate();

                var presenter = button.Presenter;
                Assert.NotNull(presenter);
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
                var button = (Button)window.Content!;

                window.ApplyTemplate();
                button.ApplyTemplate();

                var presenter = button.Presenter;
                Assert.NotNull(presenter);
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

            var parent = (ContentControl)template.Build(new ContentControl())!.Result;

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
    <ContentPresenter x:Name='PART_ContentPresenter' Content='{TemplateBinding Content}' />
</ControlTemplate>
";
            var template = AvaloniaRuntimeXamlLoader.Parse<ControlTemplate>(xaml);

            Assert.Equal(typeof(ContentControl), template.TargetType);

            Assert.IsType(typeof(ContentPresenter), template.Build(new ContentControl())!.Result);
        }

        [Fact]
        public void ControlTemplate_With_String_TargetType()
        {
            var xaml = @"
<ControlTemplate xmlns='https://github.com/avaloniaui' 
                 xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                 TargetType='ContentControl'>
    <ContentPresenter x:Name='PART_ContentPresenter' Content='{TemplateBinding Content}' />
</ControlTemplate>
";
            var template = AvaloniaRuntimeXamlLoader.Parse<ControlTemplate>(xaml);

            Assert.Equal(typeof(ContentControl), template.TargetType);

            Assert.IsType(typeof(ContentPresenter), template.Build(new ContentControl())!.Result);
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

            var panel = (Panel)template.Build(new ContentControl())!.Result;

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

        [Fact]
        public void ControlTemplate_Outputs_Error_When_Missing_TemplatePart()
        {
            using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

            var xaml = @"
<ControlTemplate xmlns='https://github.com/avaloniaui'
                 xmlns:controls='using:Avalonia.Markup.Xaml.UnitTests.Xaml'
                 TargetType='controls:CustomButtonWithParts'>
    <Border Name='PART_Typo_MainContentBorder'>
        <ContentPresenter Name='PART_ContentPresenter'
                          Content='{TemplateBinding Content}'/>
    </Border>
</ControlTemplate>";
            var diagnostics = new List<RuntimeXamlDiagnostic>();
            AvaloniaRuntimeXamlLoader.Load(new RuntimeXamlLoaderDocument(xaml), new RuntimeXamlLoaderConfiguration
            {
                LocalAssembly = typeof(XamlIlTests).Assembly,
                DiagnosticHandler = diagnostic =>
                {
                    diagnostics.Add(diagnostic);
                    return diagnostic.Severity;
                }
            });
            var warning = Assert.Single(diagnostics);
            Assert.Equal(RuntimeXamlDiagnosticSeverity.Info, warning.Severity);
            Assert.Contains("'PART_MainContentBorder'", warning.Title);
        }
        
        [Fact]
        public void ControlTemplate_Outputs_Error_When_Using_Wrong_Type_With_TemplatePart()
        {
            using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

            var xaml = @"
<ControlTemplate xmlns='https://github.com/avaloniaui'
                 xmlns:controls='using:Avalonia.Markup.Xaml.UnitTests.Xaml'
                 TargetType='controls:CustomControlWithParts'>
    <Border Name='PART_MainContentBorder'>
        <ContentControl Name='PART_ContentPresenter'
                        Content='{TemplateBinding Content}'/>
    </Border>
</ControlTemplate>";
            var diagnostics = new List<RuntimeXamlDiagnostic>();
            Assert.ThrowsAny<XmlException>(() => AvaloniaRuntimeXamlLoader.Load(new RuntimeXamlLoaderDocument(xaml), new RuntimeXamlLoaderConfiguration
            {
                LocalAssembly = typeof(XamlIlTests).Assembly,
                DiagnosticHandler = diagnostic =>
                {
                    diagnostics.Add(diagnostic);
                    return diagnostic.Severity;
                }
            }));
            var warning = Assert.Single(diagnostics);
            Assert.Equal(RuntimeXamlDiagnosticSeverity.Error, warning.Severity);
            Assert.Contains("'ContentPresenter'", warning.Title);
        }

        [Fact]
        public void ControlTemplate_Outputs_Error_When_Missing_TemplatePart_Nested_ItemTemplate_Case()
        {
            using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

            var xaml = @"
<ControlTemplate xmlns='https://github.com/avaloniaui'
                 xmlns:controls='using:Avalonia.Markup.Xaml.UnitTests.Xaml'
                 TargetType='controls:CustomControlWithParts'>
    <Border Name='PART_Typo_MainContentBorder'>
        <StackPanel>
            <ItemsControl>
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <!-- This PART_MainContentBorder shouldn't full parent ControlTemplate, PART_Typo_MainContentBorder still isn't properly named. -->
                        <Border Name='PART_MainContentBorder' />
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
            <ContentPresenter Name='PART_ContentPresenter'
                              Content='{TemplateBinding Content}'/>
        </StackPanel>
    </Border>
</ControlTemplate>";
            var diagnostics = new List<RuntimeXamlDiagnostic>();
            AvaloniaRuntimeXamlLoader.Load(new RuntimeXamlLoaderDocument(xaml), new RuntimeXamlLoaderConfiguration
            {
                LocalAssembly = typeof(XamlIlTests).Assembly,
                DiagnosticHandler = diagnostic =>
                {
                    diagnostics.Add(diagnostic);
                    return diagnostic.Severity;
                }
            });
            var warning = Assert.Single(diagnostics);
            Assert.Equal(RuntimeXamlDiagnosticSeverity.Info, warning.Severity);
            Assert.Contains("'PART_MainContentBorder'", warning.Title);
        }

        [Fact]
        public void Custom_ControlTemplate_Allows_TemplateBindings()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(
                    """
                    <Window xmlns="https://github.com/avaloniaui"
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                            xmlns:controls="using:Avalonia.Markup.Xaml.UnitTests.Xaml">
                        <Button Content="Foo">
                            <Button.Template>
                                <controls:CustomControlTemplate>
                                    <ContentPresenter Name="PART_ContentPresenter"
                                                      Content="{TemplateBinding Content}"/>
                                </controls:CustomControlTemplate>
                            </Button.Template>
                        </Button>
                    </Window>
                    """);
                var button = Assert.IsType<Button>(window.Content);

                window.ApplyTemplate();
                button.ApplyTemplate();

                var presenter = button.Presenter;
                Assert.NotNull(presenter);
                Assert.Equal("Foo", presenter.Content);
            }
        }
    }

    public class ListBoxHierarchyLine : Panel
    {
        public static readonly StyledProperty<DashStyle?> LineDashStyleProperty =
            AvaloniaProperty.Register<ListBoxHierarchyLine, DashStyle?>(nameof(LineDashStyle));

        public DashStyle? LineDashStyle
        {
            get => GetValue(LineDashStyleProperty);
            set => SetValue(LineDashStyleProperty, value);
        }
    }

    [TemplatePart("PART_MainContentBorder", typeof(Border))]
    [TemplatePart("PART_ContentPresenter", typeof(ContentPresenter))]
    public class CustomControlWithParts : ContentControl
    {
    }

    public class CustomButtonWithParts : CustomControlWithParts
    {
    }

    public class CustomControlTemplate : IControlTemplate
    {
        [Content]
        [TemplateContent]
        public object? Content { get; set; }

        public Type? TargetType { get; set; }

        public TemplateResult<Control>? Build(TemplatedControl control) => TemplateContent.Load(Content);
    }
}
