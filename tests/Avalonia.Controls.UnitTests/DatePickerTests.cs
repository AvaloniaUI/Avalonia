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
    public class DatePickerTests
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
                Threading.Dispatcher.UIThread.RunJobs();
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
                Threading.Dispatcher.UIThread.RunJobs();

                var desc = datePicker.GetVisualDescendants();
                Assert.True(desc.Count() > 1);//Should be layoutroot grid & button
                TextBlock dayText = null;
                Grid container = null;

                Assert.True(desc.ElementAt(1) is Button);

                container = (desc.ElementAt(1) as Button).Content as Grid;
                Assert.True(container != null);

                for(int i = 0; i < container.Children.Count; i++)
                {
                    if(container.Children[i] is TextBlock tb && tb.Name == "DayText")
                    {
                        dayText = tb;
                        break;
                    }
                }

                Assert.True(dayText != null);
                Assert.True(!dayText.IsVisible);
                Assert.True(container.ColumnDefinitions.Count == 3);
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
                Threading.Dispatcher.UIThread.RunJobs();

                var desc = datePicker.GetVisualDescendants();
                Assert.True(desc.Count() > 1);//Should be layoutroot grid & button
                TextBlock monthText = null;
                Grid container = null;

                Assert.True(desc.ElementAt(1) is Button);

                container = (desc.ElementAt(1) as Button).Content as Grid;
                Assert.True(container != null);

                for (int i = 0; i < container.Children.Count; i++)
                {
                    if (container.Children[i] is TextBlock tb && tb.Name == "MonthText")
                    {
                        monthText = tb;
                        break;
                    }
                }

                Assert.True(monthText != null);
                Assert.True(!monthText.IsVisible);
                Assert.True(container.ColumnDefinitions.Count == 3);
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
                Threading.Dispatcher.UIThread.RunJobs();

                var desc = datePicker.GetVisualDescendants();
                Assert.True(desc.Count() > 1);//Should be layoutroot grid & button
                TextBlock yearText = null;
                Grid container = null;

                Assert.True(desc.ElementAt(1) is Button);

                container = (desc.ElementAt(1) as Button).Content as Grid;
                Assert.True(container != null);

                for (int i = 0; i < container.Children.Count; i++)
                {
                    if (container.Children[i] is TextBlock tb && tb.Name == "YearText")
                    {
                        yearText = tb;
                        break;
                    }
                }

                Assert.True(yearText != null);
                Assert.True(!yearText.IsVisible);
                Assert.True(container.ColumnDefinitions.Count == 3);
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
                Threading.Dispatcher.UIThread.RunJobs();

                var desc = datePicker.GetVisualDescendants();
                Assert.True(desc.Count() > 1);//Should be layoutroot grid & button
                TextBlock yearText = null;
                TextBlock monthText = null;
                TextBlock dayText = null;
                Grid container = null;

                Assert.True(desc.ElementAt(1) is Button);

                container = (desc.ElementAt(1) as Button).Content as Grid;
                Assert.True(container != null);

                for (int i = 0; i < container.Children.Count; i++)
                {
                    if (container.Children[i] is TextBlock tb && tb.Name == "YearText")
                    {
                        yearText = tb;
                    }
                    else if (container.Children[i] is TextBlock tb1 && tb1.Name == "MonthText")
                    {
                        monthText = tb1;
                    }
                    else if (container.Children[i] is TextBlock tb2 && tb2.Name == "DayText")
                    {
                        dayText = tb2;
                    }
                }

                DateTimeOffset value = new DateTimeOffset(2000, 10, 10, 0, 0, 0, TimeSpan.Zero);
                datePicker.SelectedDate = value;

                Assert.False(dayText.Text == "day");
                Assert.False(monthText.Text == "month");
                Assert.False(yearText.Text == "year");
                Assert.False(datePicker.Classes.Contains(":hasnodate"));

                datePicker.SelectedDate = null;

                Assert.True(dayText.Text == "day");
                Assert.True(monthText.Text == "month");
                Assert.True(yearText.Text == "year");
                Assert.True(datePicker.Classes.Contains(":hasnodate"));
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
                    Name = "ButtonContentGrid"
                }.RegisterInNameScope(scope);
                var dayText = new TextBlock
                {
                    Name = "DayText"
                }.RegisterInNameScope(scope);
                var monthText = new TextBlock
                {
                    Name = "MonthText"
                }.RegisterInNameScope(scope);
                var yearText = new TextBlock
                {
                    Name = "YearText"
                }.RegisterInNameScope(scope);
                var firstSpacer = new Rectangle
                {
                    Name = "FirstSpacer"
                }.RegisterInNameScope(scope);
                var secondSpacer = new Rectangle
                {
                    Name = "SecondSpacer"
                }.RegisterInNameScope(scope);
               
                contentGrid.Children.AddRange(new IControl[] { dayText, monthText, yearText, firstSpacer, secondSpacer });
                flyoutButton.Content = contentGrid;
                layoutRoot.Children.Add(flyoutButton);
                return layoutRoot;
            });
        }
    }
}
