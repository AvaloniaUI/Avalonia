﻿using System;
using System.Linq;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Platform;
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

        private static TestServices Services => TestServices.MockThreadingInterface.With(
            fontManagerImpl: new MockFontManagerImpl(),
            standardCursorFactory: Mock.Of<IStandardCursorFactory>(),
            textShaperImpl: new MockTextShaperImpl());

        private IControlTemplate CreateTemplate()
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
                    Name = "FlyoutButton"
                }.RegisterInNameScope(scope);
                var contentGrid = new Grid
                {
                    Name = "FlyoutButtonContentGrid"
                }.RegisterInNameScope(scope);

                var firstPickerHost = new Border
                {
                    Name = "FirstPickerHost",
                    Child = new TextBlock
                    {
                        Name = "HourTextBlock"
                    }.RegisterInNameScope(scope)
                }.RegisterInNameScope(scope);
                Grid.SetColumn(firstPickerHost, 0);

                var secondPickerHost = new Border
                {
                    Name = "SecondPickerHost",
                    Child = new TextBlock
                    {
                        Name = "MinuteTextBlock"
                    }.RegisterInNameScope(scope)
                }.RegisterInNameScope(scope);
                Grid.SetColumn(secondPickerHost, 2);

                var thirdPickerHost = new Border
                {
                    Name = "ThirdPickerHost",
                    Child = new TextBlock
                    {
                        Name = "PeriodTextBlock"
                    }.RegisterInNameScope(scope)
                }.RegisterInNameScope(scope);
                Grid.SetColumn(thirdPickerHost, 4);

                var firstSpacer = new Rectangle
                {
                    Name = "FirstColumnDivider"
                }.RegisterInNameScope(scope);
                Grid.SetColumn(firstSpacer, 1);

                var secondSpacer = new Rectangle
                {
                    Name = "SecondColumnDivider"
                }.RegisterInNameScope(scope);
                Grid.SetColumn(secondSpacer, 3);

                contentGrid.Children.AddRange(new IControl[] { firstPickerHost, firstSpacer, secondPickerHost, secondSpacer, thirdPickerHost });
                flyoutButton.Content = contentGrid;
                layoutRoot.Children.Add(flyoutButton);
                return layoutRoot;
            });
        }
    }
}
