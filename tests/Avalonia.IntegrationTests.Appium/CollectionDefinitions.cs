using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [CollectionDefinition("Default")]
    public class DefaultCollection : ICollectionFixture<DefaultAppFixture>
    {
    }

    [CollectionDefinition("WindowDecorations")]
    public class WindowDecorationsCollection : ICollectionFixture<DefaultAppFixture>
    {
    }

    [CollectionDefinition("OverlayPopups")]
    public class OverlayPopupsCollection : ICollectionFixture<OverlayPopupsAppFixture>
    {
    }
}
