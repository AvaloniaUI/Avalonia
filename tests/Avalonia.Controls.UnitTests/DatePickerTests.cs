using System;
using System.Linq;
using System.Reactive.Subjects;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Harfbuzz;
using Avalonia.Headless;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class DatePickerTests : ScopedTestBase
    {
        [Fact]
        public void SelectedDateChanged_Should_Fire_When_SelectedDate_Set()
        {
            using (UnitTestApplication.Start(Services))
            {
                bool handled = false;
                DatePicker datePicker = new DatePicker();
                datePicker.SelectedDateChanged += (s, e) =>
                {
                    handled = true;
                };
                DateTimeOffset value = new DateTimeOffset(2000, 10, 10, 0, 0, 0, TimeSpan.Zero);
                datePicker.SelectedDate = value;
                Threading.Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                Assert.True(handled);
            }
        }

        [Fact]
        public void DayVisible_False_Should_Hide_Day()
        {
            using (UnitTestApplication.Start(Services))
            {
                DatePicker datePicker = new DatePicker
                {
                    Template = CreateTemplate(),
                    DayVisible = false
                };
                datePicker.ApplyTemplate();
                Threading.Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                var desc = datePicker.GetVisualDescendants();
                Assert.True(desc.Count() > 1);//Should be layoutroot grid & button
                TextBlock? dayText = null;

                var button = Assert.IsAssignableFrom<Button>(desc.ElementAt(1));
                var container = Assert.IsAssignableFrom<Grid>(button.Content);

                for(int i = 0; i < container.Children.Count; i++)
                {
                    if(container.Children[i] is TextBlock tb && tb.Name == "PART_DayTextBlock")
                    {
                        dayText = tb;
                        break;
                    }
                }

                Assert.NotNull(dayText);
                Assert.False(dayText.IsVisible);
                Assert.Equal(3, container.ColumnDefinitions.Count);
            }
        }

        [Fact]
        public void MonthVisible_False_Should_Hide_Month()
        {
            using (UnitTestApplication.Start(Services))
            {
                DatePicker datePicker = new DatePicker
                {
                    Template = CreateTemplate(),
                    MonthVisible = false
                };
                datePicker.ApplyTemplate();
                Threading.Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                var desc = datePicker.GetVisualDescendants();
                Assert.True(desc.Count() > 1);//Should be layoutroot grid & button
                TextBlock? monthText = null;

                var button = Assert.IsAssignableFrom<Button>(desc.ElementAt(1));
                var container = Assert.IsAssignableFrom<Grid>(button.Content);

                for (int i = 0; i < container.Children.Count; i++)
                {
                    if (container.Children[i] is TextBlock tb && tb.Name == "PART_MonthTextBlock")
                    {
                        monthText = tb;
                        break;
                    }
                }

                Assert.NotNull(monthText);
                Assert.False(monthText.IsVisible);
                Assert.Equal(3, container.ColumnDefinitions.Count);
            }
        }

        [Fact]
        public void YearVisible_False_Should_Hide_Year()
        {
            using (UnitTestApplication.Start(Services))
            {
                DatePicker datePicker = new DatePicker
                {
                    Template = CreateTemplate(),
                    YearVisible = false
                };
                datePicker.ApplyTemplate();
                Threading.Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                var desc = datePicker.GetVisualDescendants();
                Assert.True(desc.Count() > 1);//Should be layoutroot grid & button
                TextBlock? yearText = null;

                var button = Assert.IsAssignableFrom<Button>(desc.ElementAt(1));
                var container = Assert.IsAssignableFrom<Grid>(button.Content);

                for (int i = 0; i < container.Children.Count; i++)
                {
                    if (container.Children[i] is TextBlock tb && tb.Name == "PART_YearTextBlock")
                    {
                        yearText = tb;
                        break;
                    }
                }

                Assert.NotNull(yearText);
                Assert.False(yearText.IsVisible);
                Assert.Equal(3, container.ColumnDefinitions.Count);
            }
        }

        [Fact]
        public void SelectedDate_null_Should_Use_Placeholders()
        {
            using (UnitTestApplication.Start(Services))
            {
                DatePicker datePicker = new DatePicker
                {
                    Template = CreateTemplate(),
                    YearVisible = false
                };
                datePicker.ApplyTemplate();
                Threading.Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                var desc = datePicker.GetVisualDescendants();
                Assert.True(desc.Count() > 1);//Should be layoutroot grid & button
                TextBlock? yearText = null;
                TextBlock? monthText = null;
                TextBlock? dayText = null;

                var button = Assert.IsAssignableFrom<Button>(desc.ElementAt(1));
                var container = Assert.IsAssignableFrom<Grid>(button.Content);

                for (int i = 0; i < container.Children.Count; i++)
                {
                    if (container.Children[i] is TextBlock tb && tb.Name == "PART_YearTextBlock")
                    {
                        yearText = tb;
                    }
                    else if (container.Children[i] is TextBlock tb1 && tb1.Name == "PART_MonthTextBlock")
                    {
                        monthText = tb1;
                    }
                    else if (container.Children[i] is TextBlock tb2 && tb2.Name == "PART_DayTextBlock")
                    {
                        dayText = tb2;
                    }
                }

                Assert.NotNull(dayText);
                Assert.NotNull(monthText);
                Assert.NotNull(yearText);

                DateTimeOffset value = new DateTimeOffset(2000, 10, 10, 0, 0, 0, TimeSpan.Zero);
                datePicker.SelectedDate = value;

                Assert.NotNull(dayText.Text);
                Assert.NotNull(monthText.Text);
                Assert.NotNull(yearText.Text);
                Assert.False(datePicker.Classes.Contains(":hasnodate"));

                datePicker.SelectedDate = null;

                Assert.Null(dayText.Text);
                Assert.Null(monthText.Text);
                Assert.Null(yearText.Text);
                Assert.True(datePicker.Classes.Contains(":hasnodate"));
            }
        }

        [Fact]
        public void SelectedDate_EnableDataValidation()
        {
            var handled = false;
            var datePicker = new DatePicker();

            datePicker.SelectedDateChanged += (s, e) =>
            {
                var minDateTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
                var maxDateTime = new DateTimeOffset(2010, 1, 1, 0, 0, 0, TimeSpan.Zero);

                if (e.NewDate < minDateTime)
                    throw new DataValidationException($"dateTime is less than {minDateTime}");
                if (e.NewDate > maxDateTime)
                    throw new DataValidationException($"dateTime is over {maxDateTime}");

                handled = true;
            };

            // dateTime is less than
            Assert.Throws<DataValidationException>(() => datePicker.SelectedDate = new DateTimeOffset(1999, 1, 1, 0, 0, 0, TimeSpan.Zero));

            // dateTime is over
            Assert.Throws<DataValidationException>(() => datePicker.SelectedDate = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero));

            var exception = new DataValidationException("failed validation");
            var observable =
                new BehaviorSubject<BindingNotification>(new BindingNotification(exception,
                    BindingErrorType.DataValidationError));
            datePicker.Bind(DatePicker.SelectedDateProperty, observable);

            Assert.True(DataValidationErrors.GetHasErrors(datePicker));

            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
            datePicker.SelectedDate = new DateTimeOffset(2005, 5, 10, 11, 12, 13, TimeSpan.Zero);
            Assert.True(handled);
        }

        [Theory]
        [InlineData("PART_DaySelector")]
        [InlineData("PART_MonthSelector")]
        [InlineData("PART_YearSelector")]
        public void Selector_ScrollUp_Should_Work(string selectorName)
            => TestSelectorScrolling(selectorName, panel => panel.ScrollUp());

        [Theory]
        [InlineData("PART_DaySelector")]
        [InlineData("PART_MonthSelector")]
        [InlineData("PART_YearSelector")]
        public void Selector_ScrollDown_Should_Work(string selectorName)
            => TestSelectorScrolling(selectorName, panel => panel.ScrollDown());

        private static void TestSelectorScrolling(string selectorName, Action<DateTimePickerPanel> scroll)
        {
            using var app = UnitTestApplication.Start(Services);

            var presenter = new DatePickerPresenter { Template = CreatePickerTemplate() };
            presenter.ApplyTemplate();
            presenter.Measure(new Size(1000, 1000));

            var panel = presenter
                .GetVisualDescendants()
                .OfType<DateTimePickerPanel>()
                .FirstOrDefault(panel => panel.Name == selectorName);

            Assert.NotNull(panel);

            var previousOffset = panel.Offset;
            scroll(panel);
            Assert.NotEqual(previousOffset, panel.Offset);
        }

        private static TestServices Services => TestServices.MockThreadingInterface.With(
            fontManagerImpl: new HeadlessFontManagerStub(),
            standardCursorFactory: Mock.Of<ICursorFactory>(),
            textShaperImpl: new HarfBuzzTextShaper(),
            renderInterface: new HeadlessPlatformRenderInterface());

        private static IControlTemplate CreateTemplate()
        {
            return new FuncControlTemplate((control, scope) =>
            {
                var layoutRoot = new Grid
                {
                    Name = "LayoutRoot"
                }.RegisterInNameScope(scope);
                //Skip contentpresenter
                var flyoutButton = new Button
                {
                    Name = "PART_FlyoutButton"
                }.RegisterInNameScope(scope);
                var contentGrid = new Grid
                {
                    Name = "PART_ButtonContentGrid"
                }.RegisterInNameScope(scope);
                var dayText = new TextBlock
                {
                    Name = "PART_DayTextBlock"
                }.RegisterInNameScope(scope);
                var monthText = new TextBlock
                {
                    Name = "PART_MonthTextBlock"
                }.RegisterInNameScope(scope);
                var yearText = new TextBlock
                {
                    Name = "PART_YearTextBlock"
                }.RegisterInNameScope(scope);
                var firstSpacer = new Rectangle
                {
                    Name = "PART_FirstSpacer"
                }.RegisterInNameScope(scope);
                var secondSpacer = new Rectangle
                {
                    Name = "PART_SecondSpacer"
                }.RegisterInNameScope(scope);
                var thirdSpacer = new Rectangle
                {
                    Name = "PART_ThirdSpacer"
                }.RegisterInNameScope(scope);
               
                contentGrid.Children.AddRange(new Control[] { dayText, monthText, yearText, firstSpacer, secondSpacer, thirdSpacer });
                flyoutButton.Content = contentGrid;
                layoutRoot.Children.Add(flyoutButton);
                return layoutRoot;
            });
        }

        private static IControlTemplate CreatePickerTemplate()
        {
            return new FuncControlTemplate((_, scope) =>
            {
                var dayHost = new Panel
                {
                    Name = "PART_DayHost"
                }.RegisterInNameScope(scope);

                var daySelector = new DateTimePickerPanel
                {
                    Name = "PART_DaySelector",
                    PanelType = DateTimePickerPanelType.Day,
                    ShouldLoop = true
                }.RegisterInNameScope(scope);

                var monthHost = new Panel
                {
                    Name = "PART_MonthHost"
                }.RegisterInNameScope(scope);

                var monthSelector = new DateTimePickerPanel
                {
                    Name = "PART_MonthSelector",
                    PanelType = DateTimePickerPanelType.Month,
                    ShouldLoop = true
                }.RegisterInNameScope(scope);

                var yearHost = new Panel
                {
                    Name = "PART_YearHost"
                }.RegisterInNameScope(scope);

                var yearSelector = new DateTimePickerPanel
                {
                    Name = "PART_YearSelector",
                    PanelType = DateTimePickerPanelType.Year,
                    ShouldLoop = true
                }.RegisterInNameScope(scope);

                var acceptButton = new Button
                {
                    Name = "PART_AcceptButton"
                }.RegisterInNameScope(scope);

                var pickerContainer = new Grid
                {
                    Name = "PART_PickerContainer"
                }.RegisterInNameScope(scope);

                var firstSpacer = new Rectangle
                {
                    Name = "PART_FirstSpacer"
                }.RegisterInNameScope(scope);

                var secondSpacer = new Rectangle
                {
                    Name = "PART_SecondSpacer"
                }.RegisterInNameScope(scope);

                var contentPanel = new Panel();
                contentPanel.Children.AddRange([
                    dayHost, daySelector, monthHost, monthSelector, yearHost, yearSelector,
                    acceptButton, pickerContainer, firstSpacer, secondSpacer
                ]);
                return contentPanel;
            });
        }
    }
}
