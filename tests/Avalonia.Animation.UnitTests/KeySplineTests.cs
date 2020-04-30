using System;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace Avalonia.Animation.UnitTests
{
    public class KeySplineTests
    {
        [Theory]
        [InlineData("1,2 3,4")]
        [InlineData("1 2 3 4")]
        [InlineData("1 2,3 4")]
        [InlineData("1,2,3,4")]
        public void Can_Parse_KeySpline_Via_TypeConverter(string input)
        {
            var conv = new KeySplineTypeConverter();

            var keySpline = (KeySpline)conv.ConvertFrom(input);

            Assert.Equal(1, keySpline.ControlPointX1);
            Assert.Equal(2, keySpline.ControlPointY1);
            Assert.Equal(3, keySpline.ControlPointX2);
            Assert.Equal(4, keySpline.ControlPointY2);
        }

        [Theory]
        [InlineData(0.00)]
        [InlineData(0.50)]
        [InlineData(1.00)]
        public void KeySpline_X_Values_In_Range_Do_Not_Throw(double input)
        {
            var keySpline = new KeySpline();
            keySpline.ControlPointX1 = input; // no exception will be thrown -- test will fail if exception thrown
            keySpline.ControlPointX2 = input; // no exception will be thrown -- test will fail if exception thrown
        }

        [Theory]
        [InlineData(-0.01)]
        [InlineData(1.01)]
        public void KeySpline_X_Values_Cannot_Be_Out_Of_Range(double input)
        {
            var keySpline = new KeySpline();
            Assert.Throws<ArgumentException>(() => keySpline.ControlPointX1 = input);
            Assert.Throws<ArgumentException>(() => keySpline.ControlPointX2 = input);
        }

        [Fact]
        public void Check_KeySpline_Handled_properly()
        {
            var keyframe1 = new KeyFrame()
            {
                Setters =
                {
                    new Setter(RotateTransform.AngleProperty, -2.5d),
                },
                KeyTime = TimeSpan.FromSeconds(0)
            };

            var keyframe2 = new KeyFrame()
            {
                Setters =
                {
                    new Setter(RotateTransform.AngleProperty, 2.5d),
                },
                KeyTime = TimeSpan.FromSeconds(5),
                KeySpline = new KeySpline(0.1123555056179775,
                                          0.657303370786517,
                                          0.8370786516853934,
                                          0.499999999999999999)
            };

            var animation = new Animation()
            {
                Duration = TimeSpan.FromSeconds(5),
                Children =
                {
                    keyframe1,
                    keyframe2
                },
                IterationCount = new IterationCount(5),
                PlaybackDirection = PlaybackDirection.Alternate
            };

            var rotateTransform = new RotateTransform(-2.5);
            var rect = new Rectangle()
            {
                RenderTransform = rotateTransform
            };

            var clock = new TestClock();
            var animationRun = animation.RunAsync(rect, clock);

            // position is what you'd expect at end and beginning
            clock.Step(TimeSpan.Zero);
            Assert.Equal(rotateTransform.Angle, -2.5);
            clock.Step(TimeSpan.FromSeconds(5));
            Assert.Equal(rotateTransform.Angle, 2.5);

            // test some points in between end and beginning
            var tolerance = 0.01;
            clock.Step(TimeSpan.Parse("00:00:10.0153932"));
            var expected = -2.4122350198982545;
            Assert.True(Math.Abs(rotateTransform.Angle - expected) <= tolerance);

            clock.Step(TimeSpan.Parse("00:00:11.2655407"));
            expected = -0.37153223002125113;
            Assert.True(Math.Abs(rotateTransform.Angle - expected) <= tolerance);

            clock.Step(TimeSpan.Parse("00:00:12.6158773"));
            expected = 0.3967885416786294;
            Assert.True(Math.Abs(rotateTransform.Angle - expected) <= tolerance);

            clock.Step(TimeSpan.Parse("00:00:14.6495256"));
            expected = 1.8016358493761722;
            Assert.True(Math.Abs(rotateTransform.Angle - expected) <= tolerance);
        }
    }
}
