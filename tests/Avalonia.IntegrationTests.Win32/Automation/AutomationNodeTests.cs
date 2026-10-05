using System.Threading.Tasks;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Win32.Automation;
using Xunit;
using UIA = Avalonia.Win32.Automation.Interop;

namespace Avalonia.IntegrationTests.Win32.Automation;

public class AutomationNodeTests
{
    [Fact]
    public async Task Toggle_Pattern_Is_Available_For_Checkable_MenuItem_From_Another_Thread()
    {
        var node = await CreateMenuItemNodeAsync(isChecked: false);

        var provider = await Task.Run(
            () => node.GetPatternProvider((int)UIA.UiaPatternId.Toggle),
            TestContext.Current.CancellationToken);

        Assert.Same(node, provider);
    }

    [Fact]
    public async Task Toggle_State_Of_Checkable_MenuItem_Can_Be_Read_From_Another_Thread()
    {
        var node = await CreateMenuItemNodeAsync(isChecked: true);

        var state = await Task.Run(
            () => ((UIA.IToggleProvider)node).GetToggleState(),
            TestContext.Current.CancellationToken);

        Assert.Equal(ToggleState.On, state);
    }

    private static async Task<AutomationNode> CreateMenuItemNodeAsync(bool isChecked)
    {
        return await Dispatcher.UIThread.InvokeAsync(
            () => new AutomationNode(new MenuItemAutomationPeer(new MenuItem
            {
                ToggleType = MenuItemToggleType.CheckBox,
                IsChecked = isChecked
            })),
            DispatcherPriority.Normal,
            TestContext.Current.CancellationToken);
    }
}
