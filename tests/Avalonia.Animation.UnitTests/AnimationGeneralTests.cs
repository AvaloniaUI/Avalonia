using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.Data;
using Xunit;
using Avalonia.Animation.Easings;
using Avalonia.Data.Core;
using Avalonia.Media;

namespace Avalonia.Animation.UnitTests
{
    public class AnimationGeneralTests
    {
        [Fact]
        public void Test_Color_Animations_Single()
        {
            // initialize SCB handler
            var initSCB = new SolidColorBrush();

            var kf1 = new KeyFrame()
            {
                Setters =
                {
                    new Setter(null, Colors.White)
                    { PropertyPath = new PropertyPathBuilder().Property(Border.BackgroundProperty).Build() }
                },
                Cue = new Cue(0d)
            };
            var kf2 = new KeyFrame()
            {
                Setters =
                {
                    new Setter(null, Colors.Black)
                    { PropertyPath = new PropertyPathBuilder().Property(Border.BackgroundProperty).Build() }
                },
                Cue = new Cue(1d)
            };

            var animation = new Animation()
            {
                FillMode = FillMode.Both,
                Duration = TimeSpan.FromSeconds(3),
                Children =
                {
                    kf1,
                    kf2
                }
            };

            var border = new Border()
            {
            };
            var clock = new TestClock();
            var testTSO = new TestSelectorObservable();

            animation.Apply(border, clock, testTSO, () => { });

            testTSO.Toggle(true);
            clock.Step(TimeSpan.Zero);

            // Initial Delay.
            clock.Step(TimeSpan.FromSeconds(3));
            Assert.Equal(Colors.Black, ((SolidColorBrush)border.Background).Color);

        }

        public class TestSelectorObservable : IObservable<bool>, IDisposable
        {
            IObserver<bool> testObserver;

            public void Toggle(bool val)
            {
                testObserver?.OnNext(val);
            }

            public void Dispose()
            {
                testObserver?.OnCompleted();
            }

            public IDisposable Subscribe(IObserver<bool> observer)
            {
                testObserver = observer;
                return this;
            }
        }

    }
}
