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
    public class TimePickerTests : ScopedTestBase
    {
        [Fact]
        public void SelectedTimeChanged_Should_Fire_When_SelectedTime_Set()
        {
            using (UnitTestApplication.Start(Services))
            {
                bool handled = false;
                TimePicker timePicker = new TimePicker();
                timePicker.SelectedTimeChanged += (s, e) =>
                {
                    handled = true;
                };
                TimeSpan value = TimeSpan.FromHours(10);
                timePicker.SelectedTime = value;
                Threading.Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                Assert.True(handled);
            }
        }

        [Fact]
        public void Using_24HourClock_Should_Hide_Period()
        {
            using (UnitTestApplication.Start(Services))
            {
                TimePicker timePicker = new TimePicker()
                {
                    ClockIdentifier = "12HourClock",
                    Template = CreateTemplate()
                };
                timePicker.ApplyTemplate();

                var desc = timePicker.GetVisualDescendants();
                Assert.True(desc.Count() > 1);//Should be layoutroot grid & button

                var button = Assert.IsAssignableFrom<Button>(desc.ElementAt(1));
                var container = Assert.IsAssignableFrom<Grid>(button.Content);

                var periodTextHost = Assert.IsAssignableFrom<Border>(container.Children[6]);
                Assert.True(periodTextHost.IsVisible);

                timePicker.ClockIdentifier = "24HourClock";
                Assert.False(periodTextHost.IsVisible);
            }
        }
        
        [Fact]
        public void UseSeconds_Equals_False_Should_Hide_Seconds()
        {
            using (UnitTestApplication.Start(Services))
            {
                TimePicker timePicker = new TimePicker()
                {
                    UseSeconds = true,
                    Template = CreateTemplate()
                };
                timePicker.ApplyTemplate();

                var desc = timePicker.GetVisualDescendants();
                Assert.True(desc.Count() > 1);//Should be layoutroot grid & button

                var button = Assert.IsAssignableFrom<Button>(desc.ElementAt(1));
                var container = Assert.IsAssignableFrom<Grid>(button.Content);

                var periodTextHost = Assert.IsAssignableFrom<Border>(container.Children[4]);
                Assert.True(periodTextHost.IsVisible);

                timePicker.UseSeconds = false;
                Assert.False(periodTextHost.IsVisible);
            }
        }

        [Fact]
        public void UseSeconds_Equals_False_Should_Have_Zero_Seconds()
        {
            using (UnitTestApplication.Start(Services))
            {
                TimePicker timePicker = new TimePicker()
                {
                    UseSeconds = false,
                    Template = CreateTemplate(includePopup: true)
                };
                timePicker.ApplyTemplate();

                var desc = timePicker.GetVisualDescendants();
                Assert.True(desc.Count() > 2);

                // find button
                Assert.True(desc.ElementAt(1) is Button);
                var btn = (Button)desc.ElementAt(1);

                Assert.True(desc.ElementAt(2) is Popup);
                var popup = (Popup)desc.ElementAt(2);

                var timePickerPresenter = Assert.IsAssignableFrom<TimePickerPresenter>(popup.Child);
                var panel = (Panel)timePickerPresenter.VisualChildren[0];
                var acceptBtn = (Button)panel.VisualChildren[0];

                Assert.False(popup.IsOpen);
                btn.PerformClick();
                Assert.True(popup.IsOpen);
                Assert.False(timePickerPresenter.UseSeconds);

                acceptBtn.PerformClick();

                Assert.Equal(0, timePickerPresenter.Time.Seconds);
                Assert.Equal(0, timePicker.SelectedTime?.Seconds);
            }
        }

        [Fact]
        public void TimePickerPresenter_UseSeconds_Equals_False_Should_Have_Zero_Seconds()
        {
            using (UnitTestApplication.Start(Services))
            {
                TimePickerPresenter timePickerPresenter = new TimePickerPresenter()
                {
                    UseSeconds = false,
                    Template = CreatePickerTemplate(),
                };
                timePickerPresenter.ApplyTemplate();

                var panel = (Panel)timePickerPresenter.VisualChildren[0];
                var acceptBtn = (Button)panel.VisualChildren[0];

                acceptBtn.PerformClick();

                Assert.Equal(0, timePickerPresenter.Time.Seconds);
            }
        }

        [Fact]
        public void SelectedTime_null_Should_Use_Placeholders()
        {
            using (UnitTestApplication.Start(Services))
            {
                TimePicker timePicker = new TimePicker()
                {
                    Template = CreateTemplate()
                };
                timePicker.ApplyTemplate();

                var desc = timePicker.GetVisualDescendants();
                Assert.True(desc.Count() > 1);//Should be layoutroot grid & button

                var button = Assert.IsAssignableFrom<Button>(desc.ElementAt(1));
                var container = Assert.IsAssignableFrom<Grid>(button.Content);

                var hourTextHost = Assert.IsAssignableFrom<Border>(container.Children[0]);
                var hourText = Assert.IsAssignableFrom<TextBlock>(hourTextHost.Child);
                var minuteTextHost = Assert.IsAssignableFrom<Border>(container.Children[2]);
                var minuteText = Assert.IsAssignableFrom<TextBlock>(minuteTextHost.Child);
                var secondTextHost = Assert.IsAssignableFrom<Border>(container.Children[4]);
                var secondText = Assert.IsAssignableFrom<TextBlock>(secondTextHost.Child);

                TimeSpan ts = TimeSpan.FromHours(10);
                timePicker.SelectedTime = ts;
                Assert.NotNull(hourText.Text);
                Assert.NotNull(minuteText.Text);
                Assert.NotNull(secondText.Text);

                timePicker.SelectedTime = null;
                Assert.Null(hourText.Text);
                Assert.Null(minuteText.Text);
                Assert.Null(secondText.Text);
            }
        }
        
        [Fact]
        [UseEmptyDesignatorCulture]
        public void Using_12HourClock_On_Culture_With_Empty_Period_Should_Show_Period()
        {
            using (UnitTestApplication.Start(Services))
            {
                TimePicker timePicker = new TimePicker()
                {
                    Template = CreateTemplate(), ClockIdentifier = "12HourClock",
                };
                timePicker.ApplyTemplate();

                var desc = timePicker.GetVisualDescendants();
                Assert.True(desc.Count() > 1); //Should be layoutroot grid & button

                var button = Assert.IsAssignableFrom<Button>(desc.ElementAt(1));
                var container = Assert.IsAssignableFrom<Grid>(button.Content);

                var periodTextHost = Assert.IsAssignableFrom<Border>(container.Children[6]);
                var periodText = Assert.IsAssignableFrom<TextBlock>(periodTextHost.Child);

                TimeSpan ts = TimeSpan.FromHours(10);
                timePicker.SelectedTime = ts;
                Assert.False(string.IsNullOrEmpty(periodText.Text));

                timePicker.SelectedTime = null;
                Assert.False(string.IsNullOrEmpty(periodText.Text));
            }
        }

        [Fact]
        public void SelectedTime_EnableDataValidation()
        {
            using (UnitTestApplication.Start(Services))
            {
                var handled = false;
                var timePicker = new TimePicker();

                timePicker.SelectedTimeChanged += (s, e) =>
                {
                    var minTime = new TimeSpan(10, 0, 0);
                    var maxTime = new TimeSpan(15, 0, 0);

                    if (e.NewTime < minTime)
                        throw new DataValidationException($"time is less than {maxTime}");

                    if (e.NewTime > maxTime)
                        throw new DataValidationException($"time is over {maxTime}");

                    handled = true;
                };

                // time is less than
                Assert.Throws<DataValidationException>(() => timePicker.SelectedTime = new TimeSpan(1, 2, 3));

                // time is over
                Assert.Throws<DataValidationException>(() => timePicker.SelectedTime = new TimeSpan(21, 22, 23));

                var exception = new DataValidationException("failed validation");
                var observable =
                    new BehaviorSubject<BindingNotification>(new BindingNotification(exception,
                        BindingErrorType.DataValidationError));
                timePicker.Bind(TimePicker.SelectedTimeProperty, observable);

                Assert.True(DataValidationErrors.GetHasErrors(timePicker));

                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                timePicker.SelectedTime = new TimeSpan(11, 12, 13);
                Assert.True(handled);
            }
        }

        [Theory]
        [InlineData("PART_HourSelector")]
        [InlineData("PART_MinuteSelector")]
        [InlineData("PART_SecondSelector")]
        public void Selector_ScrollUp_Should_Work(string selectorName)
            => TestSelectorScrolling(selectorName, panel => panel.ScrollUp());

        [Theory]
        [InlineData("PART_HourSelector")]
        [InlineData("PART_MinuteSelector")]
        [InlineData("PART_SecondSelector")]
        public void Selector_ScrollDown_Should_Work(string selectorName)
            => TestSelectorScrolling(selectorName, panel => panel.ScrollDown());

        private static void TestSelectorScrolling(string selectorName, Action<DateTimePickerPanel> scroll)
        {
            using var app = UnitTestApplication.Start(Services);

            var presenter = new TimePickerPresenter { Template = CreatePickerTemplate() };
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

        private static IControlTemplate CreateTemplate(bool includePopup = false)
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
                    Name = "PART_FlyoutButtonContentGrid"
                }.RegisterInNameScope(scope);

                var firstPickerHost = new Border
                {
                    Name = "PART_FirstPickerHost",
                    Child = new TextBlock
                    {
                        Name = "PART_HourTextBlock"
                    }.RegisterInNameScope(scope)
                }.RegisterInNameScope(scope);
                Grid.SetColumn(firstPickerHost, 0);

                var secondPickerHost = new Border
                {
                    Name = "PART_SecondPickerHost",
                    Child = new TextBlock
                    {
                        Name = "PART_MinuteTextBlock"
                    }.RegisterInNameScope(scope)
                }.RegisterInNameScope(scope);
                Grid.SetColumn(secondPickerHost, 2);

                var thirdPickerHost = new Border
                {
                    Name = "PART_ThirdPickerHost",
                    Child = new TextBlock
                    {
                        Name = "PART_SecondTextBlock"
                    }.RegisterInNameScope(scope)
                }.RegisterInNameScope(scope);
                Grid.SetColumn(thirdPickerHost, 4);
                
                var fourthPickerHost = new Border
                {
                    Name = "PART_FourthPickerHost",
                    Child = new TextBlock
                    {
                        Name = "PART_PeriodTextBlock"
                    }.RegisterInNameScope(scope)
                }.RegisterInNameScope(scope);
                Grid.SetColumn(fourthPickerHost, 6);

                var firstSpacer = new Rectangle
                {
                    Name = "PART_FirstColumnDivider"
                }.RegisterInNameScope(scope);
                Grid.SetColumn(firstSpacer, 1);

                var secondSpacer = new Rectangle
                {
                    Name = "PART_SecondColumnDivider"
                }.RegisterInNameScope(scope);
                Grid.SetColumn(secondSpacer, 3);

                var thirdSpacer = new Rectangle
                {
                    Name = "PART_ThirdColumnDivider"
                }.RegisterInNameScope(scope);
                Grid.SetColumn(thirdSpacer, 5);

                contentGrid.Children.AddRange(new Control[] { firstPickerHost, firstSpacer, secondPickerHost, secondSpacer, thirdPickerHost, thirdSpacer, fourthPickerHost });
                flyoutButton.Content = contentGrid;
                layoutRoot.Children.Add(flyoutButton);

                if (includePopup)
                {
                    var popup = new Popup
                    {
                        Name = "PART_Popup"
                    }.RegisterInNameScope(scope);

                    var pickerPresenter = new TimePickerPresenter
                    {
                        Name = "PART_PickerPresenter",
                        Template = CreatePickerTemplate()
                    }.RegisterInNameScope(scope);
                    pickerPresenter.ApplyTemplate();

                    popup.Child = pickerPresenter;

                    layoutRoot.Children.Add(popup);
                }

                return layoutRoot;
            });
        }

        private static IControlTemplate CreatePickerTemplate()
        {
            return new FuncControlTemplate((control, scope) =>
            {
                var acceptButton = new Button
                {
                    Name = "PART_AcceptButton"
                }.RegisterInNameScope(scope);

                var hourSelector = new DateTimePickerPanel
                {
                    Name = "PART_HourSelector",
                    PanelType = DateTimePickerPanelType.Hour,
                    ShouldLoop = true
                }.RegisterInNameScope(scope);

                var minuteSelector = new DateTimePickerPanel
                {
                    Name = "PART_MinuteSelector",
                    PanelType = DateTimePickerPanelType.Minute,
                    ShouldLoop = true
                }.RegisterInNameScope(scope);

                var secondHost = new Panel
                {
                    Name = "PART_SecondHost"
                }.RegisterInNameScope(scope);

                var secondSelector = new DateTimePickerPanel
                {
                    Name = "PART_SecondSelector",
                    PanelType = DateTimePickerPanelType.Second,
                    ShouldLoop = true
                }.RegisterInNameScope(scope);

                var periodHost = new Panel
                {
                    Name = "PART_PeriodHost"
                }.RegisterInNameScope(scope);

                var periodSelector = new DateTimePickerPanel
                {
                    Name = "PART_PeriodSelector",
                    PanelType = DateTimePickerPanelType.TimePeriod,
                }.RegisterInNameScope(scope);

                var pickerContainer = new Grid
                {
                    Name = "PART_PickerContainer"
                }.RegisterInNameScope(scope);

                var secondSpacer = new Rectangle
                {
                    Name = "PART_SecondSpacer"
                }.RegisterInNameScope(scope);

                var thirdSpacer = new Rectangle
                {
                    Name = "PART_ThirdSpacer"
                }.RegisterInNameScope(scope);

                var contentPanel = new Panel();
                contentPanel.Children.AddRange(new Control[] { acceptButton, hourSelector, minuteSelector, secondHost, secondSelector, periodHost, periodSelector, pickerContainer, secondSpacer, thirdSpacer });
                return contentPanel;
            });
        }
    }
}
