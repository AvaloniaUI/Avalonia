using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [CollectionDefinition("Default")]
    public class DefaultCollection : ICollectionFixture<DefaultAppFixture>
    {
    }

    [CollectionDefinition("OverlayPopups")]
    public class OverlayPopupsCollection : ICollectionFixture<OverlayPopupsAppFixture>
    {
    }
}
