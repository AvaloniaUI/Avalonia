using Avalonia.Controls.PullToRefresh;
using Avalonia.Input;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.PullToRefresh
{
    public class RefreshVisualizerTests : ScopedTestBase
    {
        [Fact]
        public void InteractionRatio_Is_One_While_Refreshing_And_Zero_After_Completion()
        {
            var provider = new RefreshInfoProvider(
                PullDirection.TopToBottom,
                new Size(100, 100),
                visual: null);

            var visualizer = new RefreshVisualizer();
            visualizer.RefreshInfoProvider = provider;

            var sawDuringRefresh = false;

            visualizer.RefreshRequested += (s, e) =>
            {
                Assert.Equal(1d, provider.InteractionRatio);
                sawDuringRefresh = true;
            };

            visualizer.RequestRefresh();

            Assert.True(sawDuringRefresh, "RefreshRequested event should have been raised");

            Assert.Equal(0d, provider.InteractionRatio);
        }
    }
}
