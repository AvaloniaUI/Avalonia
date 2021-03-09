using Xunit;

namespace Avalonia.IntegrationTests.Win32
{
    [CollectionDefinition("IntegrationTestApp collection")]
    public class TestAppCollection : ICollectionFixture<TestAppFixture>
    {
    }
}
