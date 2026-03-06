using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml;

public class ProvideValueTargetTests : XamlTestBase
{
    [Fact]
    public void ProvideValueTarget_Has_Correct_Targets_Set()
    {
        using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

        var capturedTargets = new CapturedTargets();
        AvaloniaLocator.CurrentMutable.BindToSelf(capturedTargets);

        AvaloniaRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'
        Foreground='{local:CapturingTargetsMarkup}'
        x:CompileBindings='True'>

  <TextBlock Tag='{Binding Source={local:CapturingTargetsMarkup}}'
             Background='{local:CapturingTargetsMarkup}' />

</Window>");

        Assert.Collection(capturedTargets.Targets,
            item =>
            {
                Assert.IsType<Window>(item.TargetObject);
                Assert.Equal(TextElement.ForegroundProperty, item.TargetProperty);
            },
            item =>
            {
                Assert.IsAssignableFrom<CompiledBindingExtension>(item.TargetObject);
                var prop = Assert.IsType<ClrPropertyInfo>(item.TargetProperty);
                Assert.Equal(nameof(Binding.Source), prop.Name);
            },
            item =>
            {
                Assert.IsType<TextBlock>(item.TargetObject);
                Assert.Equal(TextBlock.BackgroundProperty, item.TargetProperty);
            });
    }
}

public class CapturedTargets
{
    public List<(object TargetObject, object TargetProperty)> Targets { get; } = [];
}

public class CapturingTargetsMarkupExtension
{
    public object ProvideValue(IServiceProvider serviceProvider)
    {
        var parentsProvider = serviceProvider.GetRequiredService<IProvideValueTarget>();
        var capturedTargets = AvaloniaLocator.Current.GetRequiredService<CapturedTargets>();
        capturedTargets.Targets.Add((parentsProvider.TargetObject, parentsProvider.TargetProperty));
        return Brushes.DarkViolet;
    }
}
