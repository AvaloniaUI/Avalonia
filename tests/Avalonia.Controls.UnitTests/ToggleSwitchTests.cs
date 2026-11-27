using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ToggleSwitchTests : ScopedTestBase
    {
        private const double KnobTrackWidth = 40;

        [Fact]
        public void Knob_Follows_IsChecked()
        {
            var target = CreateTarget();

            target.IsChecked = true;
            Assert.Equal(KnobTrackWidth, KnobLeft(target));

            target.IsChecked = false;
            Assert.Equal(0, KnobLeft(target));
        }

        [Fact]
        public void Knob_Stays_Unchecked_When_IsCheckedChanged_Handler_Reverts_Check()
        {
            var target = CreateTarget();
            target.IsCheckedChanged += (_, _) =>
            {
                if (target.IsChecked == true)
                    target.IsChecked = false;
            };

            target.IsChecked = true;

            Assert.False(target.IsChecked);
            Assert.Contains(":unchecked", target.Classes);
            Assert.Equal(0, KnobLeft(target));
        }

        [Fact]
        public void Knob_Stays_Checked_When_IsCheckedChanged_Handler_Reverts_Uncheck()
        {
            var target = CreateTarget(isChecked: true);
            target.IsCheckedChanged += (_, _) =>
            {
                if (target.IsChecked == false)
                    target.IsChecked = true;
            };

            target.IsChecked = false;

            Assert.True(target.IsChecked);
            Assert.Contains(":checked", target.Classes);
            Assert.Equal(KnobTrackWidth, KnobLeft(target));
        }

        [Fact]
        public void Knob_Stays_Unchecked_When_Toggle_Is_Reverted_By_IsCheckedChanged_Handler()
        {
            var target = CreateTarget();
            target.IsCheckedChanged += (_, _) =>
            {
                if (target.IsChecked == true)
                    target.SetCurrentValue(ToggleSwitch.IsCheckedProperty, false);
            };

            (target as IClickableControl).RaiseClick();

            Assert.False(target.IsChecked);
            Assert.Equal(0, KnobLeft(target));
        }

        private static ToggleSwitch CreateTarget(bool isChecked = false)
        {
            var target = new ToggleSwitch
            {
                IsChecked = isChecked,
                Template = new FuncControlTemplate<ToggleSwitch>((_, scope) =>
                    new Panel
                    {
                        Name = "PART_SwitchKnob",
                        Width = KnobTrackWidth,
                        Height = 20,
                        Children =
                        {
                            new Canvas
                            {
                                Children =
                                {
                                    new Panel { Name = "PART_MovingKnobs", Width = 20, Height = 20 }
                                        .RegisterInNameScope(scope)
                                }
                            }
                        }
                    }.RegisterInNameScope(scope)),
            };

            target.ApplyTemplate();
            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(isChecked ? KnobTrackWidth : 0, KnobLeft(target));
            return target;
        }

        private static double KnobLeft(ToggleSwitch target)
        {
            foreach (var child in target.GetTemplateDescendants())
            {
                if (child.Name == "PART_MovingKnobs")
                    return Canvas.GetLeft(child);
            }

            throw new Xunit.Sdk.XunitException("PART_MovingKnobs not found");
        }
    }
}
