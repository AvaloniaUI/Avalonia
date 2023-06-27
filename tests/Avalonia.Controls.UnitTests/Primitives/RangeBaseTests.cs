using System;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Data;
using Avalonia.Styling;
using Avalonia.UnitTests;
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
            var root = new TestRoot(target);

            Assert.Equal(100, target.Minimum);
            Assert.Equal(100, target.Maximum);
        }

        [Fact]
        public void ChangingDataContextShouldNotChangeOldDataContext()
        {
            var viewModel = new RangeTestViewModel()
            {
                Minimum = -5000, 
                Maximum = 5000, 
                Value = 4000
            };
            
            var target = new TestRange
            {
                [!RangeBase.MinimumProperty] = new Binding(nameof(viewModel.Minimum)),
                [!RangeBase.MaximumProperty] = new Binding(nameof(viewModel.Maximum)),
                [!RangeBase.ValueProperty] = new Binding(nameof(viewModel.Value)),
            };
            
            var root = new TestRoot(target);
            target.DataContext = viewModel;
            target.DataContext = null;
            
            Assert.Equal(4000, viewModel.Value);
            Assert.Equal(-5000, viewModel.Minimum);
            Assert.Equal(5000, viewModel.Maximum);
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
            var root = new TestRoot(target);

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
            var root = new TestRoot(target);

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
            var root = new TestRoot(target);

            target.Maximum = 50;

            Assert.Equal(0, target.Minimum);
            Assert.Equal(50, target.Maximum);
            Assert.Equal(50, target.Value);
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
                Template = new FuncControlTemplate<RangeBase>((c, scope) =>
                {
                    track = new Track()
                    {
                        Width = 100,
                        Orientation = Orientation.Horizontal,
                        [~~Track.MinimumProperty] = c[~~RangeBase.MinimumProperty],
                        [~~Track.MaximumProperty] = c[~~RangeBase.MaximumProperty],

                        Name = "PART_Track",
                        Thumb = new Thumb()
                    }.RegisterInNameScope(scope);

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

        [Fact]
        public void Coercion_Should_Not_Be_Done_During_Initialization()
        {
            var target = new TestRange();

            target.BeginInit();

            var root = new TestRoot(target);
            target.Minimum = 1;
            Assert.Equal(0, target.Value);

            target.Value = 50;
            target.EndInit();

            Assert.Equal(50, target.Value);
        }

        [Fact]
        public void Coercion_Should_Be_Done_After_Initialization()
        {
            var target = new TestRange();

            target.BeginInit();

            var root = new TestRoot(target);
            target.Minimum = 1;

            target.EndInit();

            Assert.Equal(1, target.Value);
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

        private class RangeTestViewModel
        {
            public double Minimum { get; set; }
            public double Maximum { get; set; }
            public double Value { get; set; }
        }
    }
}
