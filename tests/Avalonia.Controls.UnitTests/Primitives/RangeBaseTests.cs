// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.ComponentModel;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Markup.Xaml.Data;
using Avalonia.Styling;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class RangeBaseTests
    {
        [Fact]
        public void Maximum_Should_Be_Coerced_To_Minimum()
        {
            var target = new TestRange
            {
                Minimum = 100,
                Maximum = 50,
            };

            Assert.Equal(100, target.Minimum);
            Assert.Equal(100, target.Maximum);
        }

        [Fact]
        public void Value_Should_Be_Coerced_To_Range()
        {
            var target = new TestRange
            {
                Minimum = 0,
                Maximum = 50,
                Value = 100,
            };

            Assert.Equal(0, target.Minimum);
            Assert.Equal(50, target.Maximum);
            Assert.Equal(50, target.Value);
        }

        [Fact]
        public void Changing_Minimum_Should_Coerce_Value_And_Maximum()
        {
            var target = new TestRange
            {
                Minimum = 0,
                Maximum = 100,
                Value = 50,
            };

            target.Minimum = 200;

            Assert.Equal(200, target.Minimum);
            Assert.Equal(200, target.Maximum);
            Assert.Equal(200, target.Value);
        }

        [Fact]
        public void Changing_Maximum_Should_Coerce_Value()
        {
            var target = new TestRange
            {
                Minimum = 0,
                Maximum = 100,
                Value = 100,
            };

            target.Maximum = 50;

            Assert.Equal(0, target.Minimum);
            Assert.Equal(50, target.Maximum);
            Assert.Equal(50, target.Value);
        }

        [Fact]
        public void Properties_Should_Not_Accept_Nan_And_Inifinity()
        {
            var target = new TestRange();

            Assert.Throws<ArgumentException>(() => target.Minimum = double.NaN);
            Assert.Throws<ArgumentException>(() => target.Minimum = double.PositiveInfinity);
            Assert.Throws<ArgumentException>(() => target.Minimum = double.NegativeInfinity);
            Assert.Throws<ArgumentException>(() => target.Maximum = double.NaN);
            Assert.Throws<ArgumentException>(() => target.Maximum = double.PositiveInfinity);
            Assert.Throws<ArgumentException>(() => target.Maximum = double.NegativeInfinity);
            Assert.Throws<ArgumentException>(() => target.Value = double.NaN);
            Assert.Throws<ArgumentException>(() => target.Value = double.PositiveInfinity);
            Assert.Throws<ArgumentException>(() => target.Value = double.NegativeInfinity);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SetValue_Should_Not_Cause_StackOverflow(bool useXamlBinding)
        {
            var viewModel = new TestStackOverflowViewModel()
            {
                Value = 50
            };

            Track track = null;

            var target = new TestRange()
            {
                Template = new FuncControlTemplate<RangeBase>(c =>
                {
                    track = new Track()
                    {
                        Width = 100,
                        Orientation = Orientation.Horizontal,
                        [~~Track.MinimumProperty] = c[~~RangeBase.MinimumProperty],
                        [~~Track.MaximumProperty] = c[~~RangeBase.MaximumProperty],

                        Name = "PART_Track",
                        Thumb = new Thumb()
                    };

                    if (useXamlBinding)
                    {
                        track.Bind(Track.ValueProperty, new Binding("Value")
                                                    {
                                                        Mode = BindingMode.TwoWay,
                                                        Source = c,
                                                        Priority = BindingPriority.Style
                                                    });
                    }
                    else
                    {
                        track[~~Track.ValueProperty] = c[~~RangeBase.ValueProperty];
                    }

                    return track;
                }),
                Minimum = 0,
                Maximum = 100,
                DataContext = viewModel
            };

            target.Bind(TestRange.ValueProperty, new Binding("Value") { Mode = BindingMode.TwoWay });

            target.ApplyTemplate();
            track.Measure(new Size(100, 0));
            track.Arrange(new Rect(0, 0, 100, 0));

            Assert.Equal(1, viewModel.SetterInvokedCount);

            // Issues #855 and #824 were causing a StackOverflowException at this point.
            target.Value = 51.001;

            Assert.Equal(2, viewModel.SetterInvokedCount);

            double expected = 51;

            Assert.Equal(expected, viewModel.Value);
            Assert.Equal(expected, target.Value);
            Assert.Equal(expected, track.Value);
        }

        private class TestRange : RangeBase
        {
        }

        private class TestStackOverflowViewModel : INotifyPropertyChanged
        {
            public int SetterInvokedCount { get; private set; }

            public const int MaxInvokedCount = 1000;

            private double _value;

            public event PropertyChangedEventHandler PropertyChanged;

            public double Value
            {
                get { return _value; }
                set
                {
                    if (_value != value)
                    {
                        SetterInvokedCount++;
                        if (SetterInvokedCount < MaxInvokedCount)
                        {
                            _value = (int)value;
                            if (_value > 75) _value = 75;
                            if (_value < 25) _value = 25;
                        }
                        else
                        {
                            _value = value;
                        }

                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                    }
                }
            }
        }
    }
}