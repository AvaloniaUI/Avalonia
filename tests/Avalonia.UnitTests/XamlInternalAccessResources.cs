namespace Avalonia.UnitTests
{
    /// <summary>
    /// Internal statics used to verify <c>{x:Static}</c> honors <c>InternalsVisibleTo</c>.
    /// Avalonia.Markup.Xaml.UnitTests is a friend assembly.
    /// </summary>
    internal static class XamlInternalAccessResources
    {
        internal static string FriendResource { get; } = "FriendResourceValue";
    }
}
