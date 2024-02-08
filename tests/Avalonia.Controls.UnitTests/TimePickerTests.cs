using System;
using System.Linq;
using System.Reactive.Subjects;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TimePickerTests
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
                Threading.Dispatcher.UIThread.RunJobs();
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
                Grid container = null;

                Assert.True(desc.ElementAt(1) is Button);

                container = (desc.ElementAt(1) as Button).Content as Grid;
                Assert.True(container != null);

                var periodTextHost = container.Children[4] as Border;
                Assert.True(periodTextHost != null);
                Assert.True(periodTextHost.IsVisible);

                timePicker.ClockIdentifier = "24HourClock";
                Assert.False(periodTextHost.IsVisible);
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
                Grid container = null;

                Assert.True(desc.ElementAt(1) is Button);

                container = (desc.ElementAt(1) as Button).Content as Grid;
                Assert.True(container != null);

                var hourTextHost = container.Children[0] as Border;
                Assert.True(hourTextHost != null);
                var hourText = hourTextHost.Child as TextBlock;
                var minuteTextHost = container.Children[2] as Border;
                Assert.True(minuteTextHost != null);
                var minuteText = minuteTextHost.Child as TextBlock;

                TimeSpan ts = TimeSpan.FromHours(10);
                timePicker.SelectedTime = ts;
                Assert.False(hourText.Text == "hour");
                Assert.False(minuteText.Text == "minute");

                timePicker.SelectedTime = null;
                Assert.True(hourText.Text == "hour");
                Assert.True(minuteText.Text == "minute");
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

                Assert.True(desc.ElementAt(1) is Button);

                var container = (desc.ElementAt(1) as Button).Content as Grid;
                Assert.True(container != null);

                var periodTextHost = container.Children[4] as Border;
                Assert.NotNull(periodTextHost);
                var periodText = periodTextHost.Child as TextBlock;
                Assert.NotNull(periodTextHost);

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

                Dispatcher.UIThread.RunJobs();
                timePicker.SelectedTime = new TimeSpan(11, 12, 13);
                Assert.True(handled);
            }
        }

        private static TestServices Services => TestServices.MockThreadingInterface.With(
            fontManagerImpl: new HeadlessFontManagerStub(),
            standardCursorFactory: Mock.Of<ICursorFactory>(),
            textShaperImpl: new HeadlessTextShaperStub(),
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
                        Name = "PART_PeriodTextBlock"
                    }.RegisterInNameScope(scope)
                }.RegisterInNameScope(scope);
                Grid.SetColumn(thirdPickerHost, 4);

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

                contentGrid.Children.AddRange(new Control[] { firstPickerHost, firstSpacer, secondPickerHost, secondSpacer, thirdPickerHost });
                flyoutButton.Content = contentGrid;
                layoutRoot.Children.Add(flyoutButton);
                return layoutRoot;
            });
        }
    }
}
