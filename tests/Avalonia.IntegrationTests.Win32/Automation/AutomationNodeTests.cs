using System;
using System.Runtime.InteropServices;
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

    [Fact]
    public async Task Invoke_Returns_Without_Waiting_For_Click_Handler_Running_Nested_Frame()
    {
        var clicked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var (node, frame) = await Dispatcher.UIThread.InvokeAsync(
            () =>
            {
                var frame = new DispatcherFrame();
                var button = new Button();

                button.Click += (_, _) =>
                {
                    clicked.TrySetResult();
                    Dispatcher.UIThread.PushFrame(frame);
                };

                return (new AutomationNode(new ButtonAutomationPeer(button)), frame);
            },
            DispatcherPriority.Normal,
            TestContext.Current.CancellationToken);

        try
        {
            var invokeTask = Task.Run(
                () => ((UIA.IInvokeProvider)node).Invoke(),
                TestContext.Current.CancellationToken);

            await invokeTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            await clicked.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }
        finally
        {
            frame.Continue = false;
        }
    }

    [Fact]
    public async Task Invoke_Throws_ElementNotEnabled_For_Disabled_Button()
    {
        var node = await Dispatcher.UIThread.InvokeAsync(
            () => new AutomationNode(new ButtonAutomationPeer(new Button { IsEnabled = false })),
            DispatcherPriority.Normal,
            TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<COMException>(
            () => Task.Run(() => ((UIA.IInvokeProvider)node).Invoke(), TestContext.Current.CancellationToken));

        Assert.Equal(UIA.UiaCoreProviderApi.UIA_E_ELEMENTNOTENABLED, exception.HResult);
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
