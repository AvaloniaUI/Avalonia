using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Avalonia.Win32.Automation;
using Avalonia.Win32.Automation.Marshalling;
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

    [Fact]
    public async Task Toggle_Throws_ElementNotEnabled_For_Disabled_ToggleButton()
    {
        var node = await Dispatcher.UIThread.InvokeAsync(
            () => new AutomationNode(new ToggleButtonAutomationPeer(new ToggleButton { IsEnabled = false })),
            DispatcherPriority.Normal,
            TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<COMException>(
            () => Task.Run(() => ((UIA.IToggleProvider)node).Toggle(), TestContext.Current.CancellationToken));

        Assert.Equal(UIA.UiaCoreProviderApi.UIA_E_ELEMENTNOTENABLED, exception.HResult);
    }

    [Fact]
    public async Task GetPropertyValue_LabeledBy_Returns_Label_AutomationNode()
    {
        var (textBoxNode, labelNode) = await Dispatcher.UIThread.InvokeAsync(
            () =>
            {
                var label = new TextBlock { Text = "Label" };
                var textBox = new TextBox();
                AutomationProperties.SetLabeledBy(textBox, label);

                var textBoxNode = AutomationNode.GetOrCreate(ControlAutomationPeer.CreatePeerForElement(textBox));
                var labelNode = AutomationNode.GetOrCreate(ControlAutomationPeer.CreatePeerForElement(label));
                return (textBoxNode, labelNode);
            },
            DispatcherPriority.Normal,
            TestContext.Current.CancellationToken);

        var value = await Task.Run(
            () => textBoxNode.GetPropertyValue((int)UIA.UiaPropertyId.LabeledBy),
            TestContext.Current.CancellationToken);

        Assert.Same(labelNode, value);

        var variant = ComVariantMarshaller.ConvertToUnmanaged(value);
        try
        {
            Assert.Equal(VarEnum.VT_UNKNOWN, variant.VarType);
            Assert.Same(labelNode, ComVariantMarshaller.ConvertToManaged(variant));
        }
        finally
        {
            ComVariantMarshaller.Free(variant);
        }
    }

    [Fact]
    public async Task GetPropertyValue_LabeledBy_Returns_Null_When_Not_Set()
    {
        var textBoxNode = await Dispatcher.UIThread.InvokeAsync(
            () => AutomationNode.GetOrCreate(ControlAutomationPeer.CreatePeerForElement(new TextBox())),
            DispatcherPriority.Normal,
            TestContext.Current.CancellationToken);

        var value = await Task.Run(
            () => textBoxNode.GetPropertyValue((int)UIA.UiaPropertyId.LabeledBy),
            TestContext.Current.CancellationToken);

        Assert.Null(value);
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
