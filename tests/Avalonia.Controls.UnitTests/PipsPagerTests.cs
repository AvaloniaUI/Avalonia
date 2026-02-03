using Avalonia.Input;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using System.Linq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class PipsPagerTests : ScopedTestBase
    {
        [Fact]
        public void NumberOfPages_Should_Update_Pips()
        {
            var target = new PipsPager();

            target.NumberOfPages = 5;

            Assert.Equal(5, target.TemplateSettings.Pips.Count);
            Assert.Equal(1, target.TemplateSettings.Pips[0]);
            Assert.Equal(5, target.TemplateSettings.Pips[4]);
        }

        [Fact]
        public void Decreasing_NumberOfPages_Should_Update_Pips()
        {
            var target = new PipsPager();
            target.NumberOfPages = 5;

            target.NumberOfPages = 3;

            Assert.Equal(3, target.TemplateSettings.Pips.Count);
        }

        [Fact]
        public void Decreasing_NumberOfPages_Should_Update_SelectedPageIndex()
        {
            var target = new PipsPager();
            target.NumberOfPages = 5;
            target.SelectedPageIndex = 4;

            target.NumberOfPages = 3;

            Assert.Equal(2, target.SelectedPageIndex);
        }

        [Fact]
        public void SelectedPageIndex_Should_Be_Clamped_To_Zero()
        {
            var target = new PipsPager();
            target.NumberOfPages = 5;

            target.SelectedPageIndex = -1;

            Assert.Equal(0, target.SelectedPageIndex);
        }

        [Fact]
        public void SelectedPageIndex_Change_Should_Raise_Event()
        {
            var target = new PipsPager();
            target.NumberOfPages = 5;
            var raised = false;
            target.SelectedIndexChanged += (s, e) => raised = true;

            target.SelectedPageIndex = 2;

            Assert.True(raised);
        }
        
        [Fact]
        public void Next_Button_Should_Increment_Index()
        {
            using var unittestApplication = UnitTestApplication.Start(TestServices.StyledWindow);

            var target = new PipsPager
            {
                NumberOfPages = 5,
                SelectedPageIndex = 1,
                IsNextButtonVisible = true,
                Template = GetTemplate()
            };
            
            var root = new TestRoot(target);
            target.ApplyTemplate();
            
            var nextButton = target.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => b.Name == "PART_NextButton");
            Assert.NotNull(nextButton);
            
            nextButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            
            Assert.Equal(2, target.SelectedPageIndex);
        }

        [Fact]
        public void Previous_Button_Should_Decrement_Index()
        {
            using var unittestApplication = UnitTestApplication.Start(TestServices.StyledWindow);

            var target = new PipsPager
            {
                NumberOfPages = 5,
                SelectedPageIndex = 3,
                IsPreviousButtonVisible = true,
                Template = GetTemplate()
            };
            
            var root = new TestRoot(target);
            target.ApplyTemplate();
            
            var prevButton = target.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => b.Name == "PART_PreviousButton");
            Assert.NotNull(prevButton);
            
            prevButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            
            Assert.Equal(2, target.SelectedPageIndex);
        }

        [Fact]
        public void Keyboard_Navigation_Should_Work()
        {
            using var unittestApplication = UnitTestApplication.Start(TestServices.StyledWindow);

            var target = new PipsPager
            {
                NumberOfPages = 5,
                SelectedPageIndex = 1,
                Orientation = Orientation.Horizontal
            };
            
            var root = new TestRoot(target);
            target.ApplyTemplate();
            
            target.RaiseEvent(new KeyEventArgs { Key = Key.Right, RoutedEvent = InputElement.KeyDownEvent });
            Assert.Equal(2, target.SelectedPageIndex);

            target.RaiseEvent(new KeyEventArgs { Key = Key.Left, RoutedEvent = InputElement.KeyDownEvent });
            Assert.Equal(1, target.SelectedPageIndex);

            target.Orientation = Orientation.Vertical;
            
            target.RaiseEvent(new KeyEventArgs { Key = Key.Down, RoutedEvent = InputElement.KeyDownEvent });
            Assert.Equal(2, target.SelectedPageIndex);

            target.RaiseEvent(new KeyEventArgs { Key = Key.Up, RoutedEvent = InputElement.KeyDownEvent });
            Assert.Equal(1, target.SelectedPageIndex);
        }

        [Fact]
        public void Orientation_PseudoClasses_Should_Be_Set()
        {
            var target = new PipsPager();

            target.Orientation = Orientation.Horizontal;
            Assert.True(target.Classes.Contains(":horizontal"));
            Assert.False(target.Classes.Contains(":vertical"));

            target.Orientation = Orientation.Vertical;
            Assert.False(target.Classes.Contains(":horizontal"));
            Assert.True(target.Classes.Contains(":vertical"));
        }

        [Fact]
        public void Clamping_Logic_Works()
        {
            var target = new PipsPager();
            target.NumberOfPages = 5;

            target.SelectedPageIndex = 10;
            Assert.Equal(4, target.SelectedPageIndex);
            
            target.SelectedPageIndex = -5;
            Assert.Equal(0, target.SelectedPageIndex);
        }

        [Fact]
        public void Manual_Button_Visibility_Should_Be_Respected()
        {
            using var unittestApplication = UnitTestApplication.Start(TestServices.StyledWindow);

            var target = new PipsPager
            {
                NumberOfPages = 5,
                IsPreviousButtonVisible = false,
                IsNextButtonVisible = false,
                Template = GetTemplate()
            };
            
            var root = new TestRoot(target);
            target.ApplyTemplate();
            
            Assert.False(target.IsPreviousButtonVisible);
            Assert.False(target.IsNextButtonVisible);

            target.IsPreviousButtonVisible = true;
            target.IsNextButtonVisible = true;
            Assert.True(target.IsPreviousButtonVisible);
            Assert.True(target.IsNextButtonVisible);
        }

        [Fact]
        public void Rapid_Page_Changes_Should_Maintain_Integrity()
        {
            var target = new PipsPager { NumberOfPages = 100 };
            var list = new System.Collections.Generic.List<int>();
            target.SelectedIndexChanged += (s, e) => list.Add(e.NewIndex);

            for (int i = 1; i <= 50; i++)
            {
                target.SelectedPageIndex = i;
            }

            Assert.Equal(50, list.Count);
            Assert.Equal(50, target.SelectedPageIndex);
            Assert.Equal(50, list.Last());
        }

        [Fact]
        public void SelectedIndexChanged_Event_Should_Have_Correct_Args()
        {
            var target = new PipsPager { NumberOfPages = 5, SelectedPageIndex = 1 };
            int oldIdx = -1;
            int newIdx = -1;
            target.SelectedIndexChanged += (s, e) =>
            {
                oldIdx = e.OldIndex;
                newIdx = e.NewIndex;
            };

            target.SelectedPageIndex = 3;
            Assert.Equal(1, oldIdx);
            Assert.Equal(3, newIdx);
        }

        [Fact]
        public void Pager_Size_Should_Update_Based_On_Orientation_And_MaxVisiblePips()
        {
            using var unittestApplication = UnitTestApplication.Start(TestServices.StyledWindow);

            var target = new PipsPager
            {
                NumberOfPages = 10,
                MaxVisiblePips = 5,
                Orientation = Orientation.Horizontal,
                Template = GetTemplate()
            };
            
            var root = new TestRoot(target);
            target.ApplyTemplate();
            
            var pipsList = target.GetVisualDescendants().OfType<ItemsControl>().First(i => i.Name == "PART_PipsPagerList");

            Assert.Equal(60, pipsList.Width);

            target.Orientation = Orientation.Vertical;
            Assert.Equal(60, pipsList.Height);
        }

        [Fact]
        public void NumberOfPages_Reduction_Should_Clamp_SelectedPageIndex()
        {
            var target = new PipsPager();
            target.NumberOfPages = 10;
            target.SelectedPageIndex = 8;
            
            target.NumberOfPages = 5;
            Assert.Equal(4, target.SelectedPageIndex);
        }

        [Fact]
        public void Page_PseudoClasses_Should_Be_Set()
        {
            var target = new PipsPager();
            target.NumberOfPages = 5;

            target.SelectedPageIndex = 0;
            Assert.True(target.Classes.Contains(":first-page"));
            Assert.False(target.Classes.Contains(":last-page"));

            target.SelectedPageIndex = 2;
            Assert.False(target.Classes.Contains(":first-page"));
            Assert.False(target.Classes.Contains(":last-page"));

            target.SelectedPageIndex = 4;
            Assert.False(target.Classes.Contains(":first-page"));
            Assert.True(target.Classes.Contains(":last-page"));
        }

        [Fact]
        public void Navigation_Buttons_IsEnabled_Should_Update()
        {
            using var unittestApplication = UnitTestApplication.Start(TestServices.StyledWindow);

            var target = new PipsPager
            {
                NumberOfPages = 3,
                Template = GetTemplate()
            };
            
            var root = new TestRoot(target);
            target.ApplyTemplate();
            
            var prevButton = target.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_PreviousButton");
            var nextButton = target.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_NextButton");

            target.SelectedPageIndex = 0;
            Assert.False(prevButton.IsEnabled);
            Assert.True(nextButton.IsEnabled);

            target.SelectedPageIndex = 1;
            Assert.True(prevButton.IsEnabled);
            Assert.True(nextButton.IsEnabled);

            target.SelectedPageIndex = 2;
            Assert.True(prevButton.IsEnabled);
            Assert.False(nextButton.IsEnabled);
        }

        [Fact]
        public void Horizontal_Keyboard_Navigation_Should_Work()
        {
            var target = new PipsPager
            {
                NumberOfPages = 5,
                SelectedPageIndex = 1,
                Orientation = Orientation.Horizontal
            };

            target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Right });
            Assert.Equal(2, target.SelectedPageIndex);

            target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Left });
            Assert.Equal(1, target.SelectedPageIndex);
        }

        [Fact]
        public void Vertical_Keyboard_Navigation_Should_Work()
        {
            var target = new PipsPager
            {
                NumberOfPages = 5,
                SelectedPageIndex = 1,
                Orientation = Orientation.Vertical
            };

            target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Down });
            Assert.Equal(2, target.SelectedPageIndex);

            target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Up });
            Assert.Equal(1, target.SelectedPageIndex);
        }

        [Fact]
        public void NumberOfPages_Zero_Should_Clamp_Index()
        {
            var target = new PipsPager();
            target.NumberOfPages = 0;
            target.SelectedPageIndex = 5;

            Assert.Equal(0, target.SelectedPageIndex);
        }

        private static FuncControlTemplate<PipsPager> GetTemplate()
        {
            return new FuncControlTemplate<PipsPager>((parent, scope) =>
            {
                return new StackPanel
                {
                    Children =
                    {
                        new Button { Name = "PART_PreviousButton" }.RegisterInNameScope(scope),
                        new ItemsControl { Name = "PART_PipsPagerList" }.RegisterInNameScope(scope),
                        new Button { Name = "PART_NextButton" }.RegisterInNameScope(scope)
                    }
                };
            });
        }

        private static FuncControlTemplate<PipsPager> GetScrollableTemplate()
        {
            return new FuncControlTemplate<PipsPager>((parent, scope) =>
            {
                return new StackPanel
                {
                    Children =
                    {
                        new Button { Name = "PART_PreviousButton" }.RegisterInNameScope(scope),
                        new ItemsControl 
                        { 
                            Name = "PART_PipsPagerList",
                            Template = new FuncControlTemplate<ItemsControl>((p, s) => 
                                new ScrollViewer 
                                { 
                                    Name = "PART_ScrollViewer",
                                    Content = new ItemsPresenter { Name = "PART_ItemsPresenter" }.RegisterInNameScope(s),
                                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
                                }.RegisterInNameScope(s))
                        }.RegisterInNameScope(scope),
                        new Button { Name = "PART_NextButton" }.RegisterInNameScope(scope)
                    }
                };
            });
        }
    }
}
