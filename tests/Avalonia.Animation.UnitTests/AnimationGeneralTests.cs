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


        [Fact]
        public void Test_SCB_Animations_Failure_Mode()
        {
            using (UnitTestApplication.Start(TestServices.RealStyler))
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

                Grid target = new Grid();
                var testClk = new TestClock();

                var root = new TestRoot
                {
                    Clock = testClk,
                    Styles = 
                    {
                        new Style(x => x.OfType<Border>().Class("Rect"))
                            {
                                Setters = new[]
                                {
                                    new Setter(
                                        Border.HeightProperty,
                                        100),
                                    new Setter(
                                        Border.WidthProperty,
                                        100),
                                },
                                Animations = 
                                {
                                    animation
                                }
                            }
                    },
                    Child = target
                };

                var b1 = new Border()
                {
                    Classes = new Classes(new string[] { "Rect" }),
                };

                var b2 = new Border()
                {
                    Classes = new Classes(new string[] { "Rect" })
                };

                target.Children.Add(b1);
                target.Children.Add(b2);

                testClk.Step(TimeSpan.Zero);
                testClk.Step(TimeSpan.FromSeconds(3)); 

                Assert.Equal(Colors.Black, ((SolidColorBrush)b1.Background).Color);
                Assert.Equal(Colors.Black, ((SolidColorBrush)b2.Background).Color);

            }
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
