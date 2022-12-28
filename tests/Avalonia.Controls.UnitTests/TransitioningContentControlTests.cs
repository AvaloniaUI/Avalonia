using System;
using Avalonia.LogicalTree;
using Avalonia.UnitTests;
using Xunit;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;

namespace Avalonia.Controls.UnitTests
{
    public class TransitioningContentControlTests
    {
        [Fact]
        public void Old_Content_Shuold_Be_Removed__From_Logical_Tree_After_Out_Animation()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var testTransition = new TestTransition();

                var target = new TransitioningContentControl();
                target.PageTransition = testTransition;

                var root = new TestRoot() { Child = target };

                var oldControl = new Control();
                var newControl = new Control();

                target.Content = oldControl;
                Threading.Dispatcher.UIThread.RunJobs();

                Assert.Equal(target, oldControl.GetLogicalParent());
                Assert.Equal(null, newControl.GetLogicalParent());

                testTransition.BeginTransition += isFrom =>
                {
                    // Old out
                    if (isFrom)
                    {
                        Assert.Equal(target, oldControl.GetLogicalParent());
                        Assert.Equal(null, newControl.GetLogicalParent());
                    }
                    // New in
                    else
                    {
                        Assert.Equal(null, oldControl.GetLogicalParent());
                        Assert.Equal(target, newControl.GetLogicalParent());
                    }
                };

                target.Content = newControl;
                Threading.Dispatcher.UIThread.RunJobs();
            }
        }
    }
    public class TestTransition : IPageTransition
    {
        public event Action<bool> BeginTransition;

        public Task Start(Visual from, Visual to, bool forward, CancellationToken cancellationToken)
        {
            bool isFrom = from != null && to == null;
            BeginTransition?.Invoke(isFrom);
            return Task.CompletedTask;
        }
    }
}
