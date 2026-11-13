using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ControlTests_FocusAdorner : ScopedTestBase
    {
        [Fact]
        public void Focusing_Control_Again_After_Focus_Moved_In_LostFocus_Should_Not_Throw()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var (target, _) = CreateStaleFocusAdorner();

                target.Focus(NavigationMethod.Tab);

                Assert.True(target.IsFocused);
                Assert.Single(AdornerLayer.GetAdornerLayer(target)!.Children);
            }
        }

        [Fact]
        public void Focusing_Control_In_New_Root_Should_Move_Stale_Focus_Adorner_To_New_AdornerLayer()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var (target, panel) = CreateStaleFocusAdorner();
                var oldLayer = AdornerLayer.GetAdornerLayer(target)!;
                var newPanel = new StackPanel();
                _ = new TestRoot { Child = new VisualLayerManager { Child = newPanel } };

                panel.Children.Remove(target);
                newPanel.Children.Add(target);
                target.Focus(NavigationMethod.Tab);

                Assert.Empty(oldLayer.Children);
                Assert.Single(AdornerLayer.GetAdornerLayer(target)!.Children);
            }
        }

        // Moving focus from a LostFocus handler makes the outer focus change raise GotFocus on the
        // target after focus has already gone elsewhere, so the target keeps its focus adorner.
        private static (Button Target, StackPanel Panel) CreateStaleFocusAdorner()
        {
            var first = new Button();
            var target = new Button { FocusAdorner = new FuncTemplate<Control>(() => new Border()) };
            var other = new Button();
            var panel = new StackPanel { Children = { first, target, other } };
            _ = new TestRoot { Child = new VisualLayerManager { Child = panel } };

            void MoveFocus(object? sender, RoutedEventArgs e)
            {
                first.LostFocus -= MoveFocus;
                other.Focus();
            }

            first.Focus();
            first.LostFocus += MoveFocus;
            target.Focus(NavigationMethod.Tab);

            Assert.True(other.IsFocused);
            Assert.Single(AdornerLayer.GetAdornerLayer(target)!.Children);
            return (target, panel);
        }
    }
}
