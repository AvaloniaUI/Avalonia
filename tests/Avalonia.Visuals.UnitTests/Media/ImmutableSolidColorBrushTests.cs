using System;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Media
{
    public class ImmutableSolidColorBrushTests
    {

        [Fact]
        public void Equals_Null_False()
        {
            var red = new ImmutableSolidColorBrush(Colors.Red);

            Assert.False(red.Equals(other: null));
            Assert.False(red.Equals(obj: null));
        }

        [Fact]
        public void Value_Equals_True()
        {
            var red1 = new ImmutableSolidColorBrush(Colors.Red);
            var red2 = new ImmutableSolidColorBrush(Colors.Red);
            var red3 = new SolidColorBrush(Colors.Red);

            Assert.True(red1.Equals(red2 as object));
            Assert.True(red1.Equals(red2 as ISolidColorBrush));

            Assert.True(red1.Equals(red3 as object));
            Assert.True(red1.Equals(red3 as ISolidColorBrush));
        }

        [Fact]
        public void Value_Equals_False()
        {
            var red1 = new ImmutableSolidColorBrush(Colors.Red);
            var red2 = new ImmutableSolidColorBrush(Colors.Red, 0.0);
            var red3 = new SolidColorBrush(Colors.Red, 0.0);

            Assert.False(red1.Equals(red2 as object));
            Assert.False(red1.Equals(red2 as ISolidColorBrush));

            Assert.False(red1.Equals(red3 as object));
            Assert.False(red1.Equals(red3 as ISolidColorBrush));
        }
    }
}
