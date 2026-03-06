#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml;

public class ParentStackProviderTests : XamlTestBase
{
    [Fact]
    public void Parents_Are_Correct_For_Deferred_Content()
    {
        using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

        var capturedParents = new CapturedParents();
        AvaloniaLocator.CurrentMutable.BindToSelf(capturedParents);

        var window = (Window)AvaloniaRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>

  <Window.Resources>
    <SolidColorBrush x:Key='Brush' Color='{local:CapturingParentsMarkupExtension}' />
  </Window.Resources>

  <TextBlock Foreground='{StaticResource Brush}' />

</Window>");

        window.Show();

        VerifyParents(capturedParents.LazyParents);
        VerifyParents(capturedParents.EagerParents);

        static void VerifyParents(object[]? parents)
        {
            Assert.NotNull(parents);
            Assert.NotEmpty(parents);
            Assert.Collection(
                parents,
                o => Assert.IsType<SolidColorBrush>(o),
                o => Assert.IsType<Window>(o),
                o => Assert.IsType<UnitTestApplication>(o));
        }
    }
}

public class CapturedParents
{
    public object[]? LazyParents { get; set; }

    public object[]? EagerParents { get; set; }
}

public class CapturingParentsMarkupExtension
{
    public object ProvideValue(IServiceProvider serviceProvider)
    {
        var parentsProvider = serviceProvider.GetRequiredService<IAvaloniaXamlIlParentStackProvider>();
        var eagerParentsProvider = Assert.IsAssignableFrom<IAvaloniaXamlIlEagerParentStackProvider>(parentsProvider);

        var capturedParents = AvaloniaLocator.Current.GetRequiredService<CapturedParents>();
        capturedParents.LazyParents = parentsProvider.Parents.ToArray();
        capturedParents.EagerParents = EnumerateEagerParents(eagerParentsProvider);

        return Colors.Blue;
    }

    private static object[] EnumerateEagerParents(IAvaloniaXamlIlEagerParentStackProvider provider)
    {
        var parents = new List<object>();

        var enumerator = new EagerParentStackEnumerator(provider);
        while (enumerator.TryGetNext() is { } parent)
            parents.Add(parent);

        return parents.ToArray();
    }
}
