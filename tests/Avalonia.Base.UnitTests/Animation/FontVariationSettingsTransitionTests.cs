using System;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Xunit;

namespace Avalonia.Base.UnitTests.Animation
{
    public class FontVariationSettingsTransitionTests
    {
        private static readonly OpenTypeTag s_wght = OpenTypeTag.Parse("wght");

        [Fact]
        public void FontVariations_Are_Interpolated_Per_Axis()
        {
            var clock = new TestClock();
            var from = FontVariationSettings.Parse("wght=400");
            var to = FontVariationSettings.Parse("wght=800");
            var target = new TextBlock { FontVariations = from };

            var sut = new FontVariationSettingsTransition
            {
                Duration = TimeSpan.FromSeconds(1),
                Property = TextBlock.FontVariationsProperty,
            };

            sut.Apply(target, clock, from, to);
            clock.Pulse(TimeSpan.Zero);
            clock.Pulse(sut.Duration * 0.5);

            Assert.NotNull(target.FontVariations);
            Assert.True(target.FontVariations.TryGetValue(s_wght, out var wght));
            Assert.Equal(600, wght);
        }

        [Fact]
        public void Mismatched_Axis_Sets_Switch_Discretely()
        {
            var clock = new TestClock();
            var from = FontVariationSettings.Parse("wght=700");
            var to = FontVariationSettings.Parse("wdth=85");
            var target = new TextBlock { FontVariations = from };

            var sut = new FontVariationSettingsTransition
            {
                Duration = TimeSpan.FromSeconds(1),
                Property = TextBlock.FontVariationsProperty,
            };

            sut.Apply(target, clock, from, to);
            clock.Pulse(TimeSpan.Zero);
            // TestClock.Pulse accumulates deltas: 0.25 then +0.35 = 0.6 of the duration.
            // Staying below 1.0 keeps the transition alive — at completion it disposes
            // and the property reverts to its base value.
            clock.Pulse(sut.Duration * 0.25);

            Assert.Equal(from, target.FontVariations);

            clock.Pulse(sut.Duration * 0.35);

            Assert.Equal(to, target.FontVariations);
        }
    }
}
