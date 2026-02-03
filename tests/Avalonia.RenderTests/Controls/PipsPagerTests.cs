using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using Avalonia.Styling;
using Avalonia.Threading;
using Xunit;

namespace Avalonia.Skia.RenderTests
{
    public class PipsPagerTests : TestBase
    {
        public PipsPagerTests()
            : base(@"Controls/PipsPager")
        {
        }
        
        private static IControlTemplate CreatePipsPagerTemplate()
        {
            return new FuncControlTemplate<PipsPager>((control, scope) =>
            {
                var stackPanel = new StackPanel
                {
                    Name = "PART_RootPanel",
                    Spacing = 5,
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

                // Simple ListBox Template (ItemsPresenter)
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

                // Default Item Style
                var itemStyle = new Style(x => x.OfType<ListBoxItem>());
                itemStyle.Setters.Add(new Setter(ListBoxItem.TemplateProperty, new FuncControlTemplate<ListBoxItem>((item, s) => 
                     new Ellipse { Name="Pip", Width = 10, Height = 10 }.RegisterInNameScope(s))));
                
                // Default Pip Fill Style
                var defaultPipStyle = new Style(x => x.OfType<ListBoxItem>().Template().Name("Pip"));
                defaultPipStyle.Setters.Add(new Setter(Ellipse.FillProperty, Brushes.Gray));
                
                // Selected Item Style
                var selectedStyle = new Style(x => x.OfType<ListBoxItem>().Class(":selected").Template().Name("Pip"));
                selectedStyle.Setters.Add(new Setter(Ellipse.FillProperty, Brushes.Red));
                
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
            Dispatcher.UIThread.RunJobs();

            await RenderToFile(target);
            CompareImages();
        }
    }
}
