using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Controls
#endif
{
    public class PipsPagerTests : TestBase
    {
        public PipsPagerTests()
            : base(@"Controls\PipsPager")
        {
        }

        private static IControlTemplate CreatePipsPagerTemplate()
        {
            return new FuncControlTemplate<PipsPager>((control, scope) =>
            {
                var stackPanel = new StackPanel
                {
                    Name = "PART_RootPanel",
                    Spacing = 4,
                    [!StackPanel.OrientationProperty] = control[!PipsPager.OrientationProperty],
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var buttonTemplate = new FuncControlTemplate<Button>((b, s) =>
                    new Border
                    {
                        Background = Brushes.LightGray,
                        Child = new TextBlock
                        {
                            [!TextBlock.TextProperty] = b[!Button.ContentProperty],
                            FontFamily = TestFontFamily,
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center
                        }
                    });

                var prevButton = new Button
                {
                    Name = "PART_PreviousButton",
                    Content = "<",
                    Template = buttonTemplate,
                    Width = 20,
                    Height = 20,
                    [!Button.IsVisibleProperty] = control[!PipsPager.IsPreviousButtonVisibleProperty]
                }.RegisterInNameScope(scope);

                var nextButton = new Button
                {
                    Name = "PART_NextButton",
                    Content = ">",
                    Template = buttonTemplate,
                    Width = 20,
                    Height = 20,
                    [!Button.IsVisibleProperty] = control[!PipsPager.IsNextButtonVisibleProperty]
                }.RegisterInNameScope(scope);

                var listBoxTemplate = new FuncControlTemplate<ListBox>((lb, s) =>
                    new ItemsPresenter
                    {
                        Name = "PART_ItemsPresenter",
                        [~ItemsPresenter.ItemsPanelProperty] = lb[~ListBox.ItemsPanelProperty],
                    }.RegisterInNameScope(s));

                var pipsList = new ListBox
                {
                    Name = "PART_PipsPagerList",
                    Template = listBoxTemplate,
                    [!ListBox.ItemsSourceProperty] = new Binding("TemplateSettings.Pips") { Source = control },
                    [!ListBox.SelectedIndexProperty] = control[!PipsPager.SelectedPageIndexProperty],
                    ItemsPanel = new FuncTemplate<Panel?>(() => new StackPanel
                    {
                        Spacing = 2,
                        [!StackPanel.OrientationProperty] = control[!PipsPager.OrientationProperty]
                    })
                }.RegisterInNameScope(scope);

                var itemStyle = new Style(x => x.OfType<ListBoxItem>());
                itemStyle.Setters.Add(new Setter(ListBoxItem.TemplateProperty,
                    new FuncControlTemplate<ListBoxItem>((item, s) =>
                        new Rectangle { Name = "Pip", Width = 10, Height = 10 }.RegisterInNameScope(s))));

                var defaultPipStyle = new Style(x => x.OfType<ListBoxItem>().Template().Name("Pip"));
                defaultPipStyle.Setters.Add(new Setter(Rectangle.FillProperty, Brushes.Gray));

                var selectedStyle = new Style(x => x.OfType<ListBoxItem>().Class(":selected").Template().Name("Pip"));
                selectedStyle.Setters.Add(new Setter(Rectangle.FillProperty, Brushes.Red));

                pipsList.Styles.Add(itemStyle);
                pipsList.Styles.Add(defaultPipStyle);
                pipsList.Styles.Add(selectedStyle);

                stackPanel.Children.Add(prevButton);
                stackPanel.Children.Add(pipsList);
                stackPanel.Children.Add(nextButton);

                return stackPanel;
            });
        }

        [Fact]
        public async Task PipsPager_Default()
        {
            var pipsPager = new PipsPager
            {
                Template = CreatePipsPagerTemplate(),
                NumberOfPages = 5,
                SelectedPageIndex = 1
            };

            var target = new Border
            {
                Padding = new Thickness(20),
                Background = Brushes.White,
                Child = pipsPager,
                Width = 400,
                Height = 150
            };

            target.Measure(new Size(400, 150));
            target.Arrange(new Rect(0, 0, 400, 150));
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task PipsPager_Preselected_Index()
        {
            var pipsPager = new PipsPager
            {
                Template = CreatePipsPagerTemplate(),
                NumberOfPages = 5,
                SelectedPageIndex = 3
            };

            var target = new Border
            {
                Padding = new Thickness(20),
                Background = Brushes.White,
                Child = pipsPager,
                Width = 400,
                Height = 150
            };

            target.Measure(new Size(400, 150));
            target.Arrange(new Rect(0, 0, 400, 150));
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }
    }
}
