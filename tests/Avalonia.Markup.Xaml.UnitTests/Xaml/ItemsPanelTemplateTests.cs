using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml;

public class ItemsPanelTemplateTests
{
    [Fact]
    public void ItemsPanelTemplate_In_Style_Allows_TemplateBinding()
    {
        using (UnitTestApplication.Start(TestServices.StyledWindow))
        {
            var window = (Window)AvaloniaRuntimeXamlLoader.Load(
                """
                <Window xmlns="https://github.com/avaloniaui"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                    <Window.Styles>
                        <Style Selector="ListBox">
                            <Setter Property="Template">
                                <ControlTemplate>
                                    <ItemsPresenter Name="PART_ItemsPresenter"
                                                    ItemsPanel="{TemplateBinding ItemsPanel}" />
                                </ControlTemplate>
                            </Setter>
                            <Setter Property="ItemsPanel">
                                <ItemsPanelTemplate>
                                    <Panel Background="{TemplateBinding Background}"
                                           Tag="{TemplateBinding ItemsSource}" />
                                </ItemsPanelTemplate>
                            </Setter>
                        </Style>
                    </Window.Styles>
                    <ListBox Background="DodgerBlue" />
                </Window>
                """);
            var listBox = Assert.IsType<ListBox>(window.Content);
            var items = new[] { "foo", "bar" };
            listBox.ItemsSource = items;

            window.ApplyTemplate();
            listBox.ApplyTemplate();

            var itemsPresenter = listBox.FindDescendantOfType<ItemsPresenter>();
            Assert.NotNull(itemsPresenter);
            itemsPresenter.ApplyTemplate();

            var panel = itemsPresenter.Panel;
            Assert.NotNull(panel);
            Assert.Equal(Brushes.DodgerBlue, panel.Background);
            Assert.Same(items, panel.Tag);
        }
    }

    [Fact]
    public void ItemsPanelTemplate_In_Control_Allows_TemplateBinding()
    {
        using (UnitTestApplication.Start(TestServices.StyledWindow))
        {
            var window = (Window)AvaloniaRuntimeXamlLoader.Load(
                """
                <Window xmlns="https://github.com/avaloniaui"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                    <ListBox Background="DodgerBlue">
                        <ListBox.Template>
                            <ControlTemplate>
                                <ItemsPresenter Name="PART_ItemsPresenter"
                                                ItemsPanel="{TemplateBinding ItemsPanel}" />
                            </ControlTemplate>
                        </ListBox.Template>
                        <ListBox.ItemsPanel>
                            <ItemsPanelTemplate>
                                <Panel Background="{TemplateBinding Background}"
                                       Tag="{TemplateBinding ItemsSource}" />
                            </ItemsPanelTemplate>
                        </ListBox.ItemsPanel>
                    </ListBox>
                </Window>
                """);
            var listBox = Assert.IsType<ListBox>(window.Content);
            var items = new[] { "foo", "bar" };
            listBox.ItemsSource = items;

            window.ApplyTemplate();
            listBox.ApplyTemplate();

            var itemsPresenter = listBox.FindDescendantOfType<ItemsPresenter>();
            Assert.NotNull(itemsPresenter);
            itemsPresenter.ApplyTemplate();

            var panel = itemsPresenter.Panel;
            Assert.NotNull(panel);
            Assert.Equal(Brushes.DodgerBlue, panel.Background);
            Assert.Same(items, panel.Tag);
        }
    }
}
