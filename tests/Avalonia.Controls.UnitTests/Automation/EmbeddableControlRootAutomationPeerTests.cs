using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.UnitTests;
using Xunit;

#nullable enable

namespace Avalonia.Controls.UnitTests.Automation
{
    public class EmbeddableControlRootAutomationPeerTests : ScopedTestBase
    {
        [Fact]
        public void Peer_Provides_IRootProvider()
        {
            using var services = new CompositorTestServices();
            var peer = ControlAutomationPeer.CreatePeerForElement(services.TopLevel);

            var rootProvider = peer.GetProvider<IRootProvider>();

            Assert.NotNull(rootProvider);
            Assert.Same(peer, rootProvider);
        }

        [Fact]
        public void Peer_Still_Provides_IEmbeddedRootProvider()
        {
            using var services = new CompositorTestServices();
            var peer = ControlAutomationPeer.CreatePeerForElement(services.TopLevel);

            var embeddedRootProvider = peer.GetProvider<IEmbeddedRootProvider>();

            Assert.NotNull(embeddedRootProvider);
        }

        [Fact]
        public void IRootProvider_PlatformImpl_Returns_Owner_PlatformImpl()
        {
            using var services = new CompositorTestServices();
            var peer = ControlAutomationPeer.CreatePeerForElement(services.TopLevel);

            var rootProvider = peer.GetProvider<IRootProvider>();

            Assert.NotNull(rootProvider);
            Assert.Same(services.TopLevel.PlatformImpl, rootProvider!.PlatformImpl);
        }
    }
}
