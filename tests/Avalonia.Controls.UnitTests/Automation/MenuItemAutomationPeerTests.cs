using System;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation;

public class MenuItemAutomationPeerTests : ScopedTestBase
{
    [Fact]
    public void Toggle_Provider_Is_Not_Exposed_When_ToggleType_None()
    {
        var peer = ControlAutomationPeer.CreatePeerForElement(new MenuItem());

        Assert.Null(peer.GetProvider<IToggleProvider>());
    }

    [Theory]
    [InlineData(MenuItemToggleType.CheckBox)]
    [InlineData(MenuItemToggleType.Radio)]
    public void Toggle_Provider_Is_Exposed_For_Checkable_Items(MenuItemToggleType toggleType)
    {
        var peer = ControlAutomationPeer.CreatePeerForElement(new MenuItem { ToggleType = toggleType });

        Assert.NotNull(peer.GetProvider<IToggleProvider>());
    }

    [Fact]
    public void ToggleState_Reflects_IsChecked()
    {
        var menuItem = new MenuItem { ToggleType = MenuItemToggleType.CheckBox };
        var provider = GetProvider(menuItem);

        Assert.Equal(ToggleState.Off, provider.ToggleState);
        menuItem.IsChecked = true;
        Assert.Equal(ToggleState.On, provider.ToggleState);
    }

    [Fact]
    public void Toggle_Flips_CheckBox_Item()
    {
        var menuItem = new MenuItem { ToggleType = MenuItemToggleType.CheckBox };
        var provider = GetProvider(menuItem);

        provider.Toggle();
        Assert.True(menuItem.IsChecked);

        provider.Toggle();
        Assert.False(menuItem.IsChecked);
    }

    [Fact]
    public void Toggle_Checks_But_Does_Not_Uncheck_Radio_Item()
    {
        var menuItem = new MenuItem { ToggleType = MenuItemToggleType.Radio };
        var provider = GetProvider(menuItem);

        provider.Toggle();
        Assert.True(menuItem.IsChecked);

        provider.Toggle();
        Assert.True(menuItem.IsChecked);
    }

    [Fact]
    public void Toggle_Raises_ToggleState_PropertyChanged()
    {
        var menuItem = new MenuItem { ToggleType = MenuItemToggleType.CheckBox };
        var peer = ControlAutomationPeer.CreatePeerForElement(menuItem);
        var provider = GetProvider(menuItem);

        var raised = 0;
        peer.PropertyChanged += (_, e) =>
        {
            if (e.Property == TogglePatternIdentifiers.ToggleStateProperty)
            {
                Assert.Equal(ToggleState.Off, e.OldValue);
                Assert.Equal(ToggleState.On, e.NewValue);
                raised++;
            }
        };

        provider.Toggle();

        Assert.Equal(1, raised);
    }

    [Fact]
    public void Leaf_Item_Exposes_Invoke_But_Not_ExpandCollapse()
    {
        var peer = ControlAutomationPeer.CreatePeerForElement(new MenuItem());

        Assert.NotNull(peer.GetProvider<IInvokeProvider>());
        Assert.Null(peer.GetProvider<IExpandCollapseProvider>());
    }

    [Fact]
    public void Submenu_Item_Exposes_ExpandCollapse_But_Not_Invoke()
    {
        var peer = ControlAutomationPeer.CreatePeerForElement(CreateSubmenuItem());

        Assert.NotNull(peer.GetProvider<IExpandCollapseProvider>());
        Assert.Null(peer.GetProvider<IInvokeProvider>());
    }

    [Fact]
    public void Providers_Follow_Items_Being_Added_And_Removed()
    {
        var menuItem = new MenuItem();
        var peer = ControlAutomationPeer.CreatePeerForElement(menuItem);

        menuItem.Items.Add(new MenuItem());

        Assert.NotNull(peer.GetProvider<IExpandCollapseProvider>());
        Assert.Null(peer.GetProvider<IInvokeProvider>());

        menuItem.Items.Clear();

        Assert.NotNull(peer.GetProvider<IInvokeProvider>());
        Assert.Null(peer.GetProvider<IExpandCollapseProvider>());
    }

    [Fact]
    public void ExpandCollapseState_Is_LeafNode_For_Leaf_Item()
    {
        var peer = (IExpandCollapseProvider)ControlAutomationPeer.CreatePeerForElement(new MenuItem());

        Assert.Equal(ExpandCollapseState.LeafNode, peer.ExpandCollapseState);
        Assert.False(peer.ShowsMenu);
    }

    [Fact]
    public void ExpandCollapseState_Reflects_IsSubMenuOpen()
    {
        var menuItem = CreateSubmenuItem();
        var provider = GetExpandCollapseProvider(menuItem);

        Assert.Equal(ExpandCollapseState.Collapsed, provider.ExpandCollapseState);
        Assert.True(provider.ShowsMenu);

        menuItem.IsSubMenuOpen = true;
        Assert.Equal(ExpandCollapseState.Expanded, provider.ExpandCollapseState);

        menuItem.IsSubMenuOpen = false;
        Assert.Equal(ExpandCollapseState.Collapsed, provider.ExpandCollapseState);
    }

    [Fact]
    public void Expand_And_Collapse_Set_IsSubMenuOpen()
    {
        var menuItem = CreateSubmenuItem();
        var provider = GetExpandCollapseProvider(menuItem);

        provider.Expand();
        Assert.True(menuItem.IsSubMenuOpen);

        provider.Collapse();
        Assert.False(menuItem.IsSubMenuOpen);
    }

    [Fact]
    public void ExpandCollapse_Raises_PropertyChanged()
    {
        var menuItem = CreateSubmenuItem();
        var peer = ControlAutomationPeer.CreatePeerForElement(menuItem);
        var provider = GetExpandCollapseProvider(menuItem);
        var raised = new List<AutomationPropertyChangedEventArgs>();

        peer.PropertyChanged += (_, e) =>
        {
            if (e.Property == ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty)
                raised.Add(e);
        };

        provider.Expand();
        provider.Collapse();

        Assert.Equal(2, raised.Count);
        Assert.Equal(ExpandCollapseState.Collapsed, raised[0].OldValue);
        Assert.Equal(ExpandCollapseState.Expanded, raised[0].NewValue);
        Assert.Equal(ExpandCollapseState.Expanded, raised[1].OldValue);
        Assert.Equal(ExpandCollapseState.Collapsed, raised[1].NewValue);
    }

    [Fact]
    public void Expand_Throws_For_Leaf_Item()
    {
        var peer = (IExpandCollapseProvider)ControlAutomationPeer.CreatePeerForElement(new MenuItem());

        Assert.Throws<InvalidOperationException>(() => peer.Expand());
    }

    [Fact]
    public void Expand_Throws_When_Disabled()
    {
        var menuItem = CreateSubmenuItem();
        menuItem.IsEnabled = false;
        var provider = GetExpandCollapseProvider(menuItem);

        Assert.Throws<ElementNotEnabledException>(() => provider.Expand());
        Assert.False(menuItem.IsSubMenuOpen);
    }

    [Fact]
    public void Collapse_Throws_For_Leaf_Item()
    {
        var peer = (IExpandCollapseProvider)ControlAutomationPeer.CreatePeerForElement(new MenuItem());

        Assert.Throws<InvalidOperationException>(() => peer.Collapse());
    }

    [Fact]
    public void Collapse_Throws_When_Disabled()
    {
        var menuItem = CreateSubmenuItem();
        menuItem.IsEnabled = false;
        menuItem.IsSubMenuOpen = true;
        var provider = GetExpandCollapseProvider(menuItem);

        Assert.Throws<ElementNotEnabledException>(() => provider.Collapse());
        Assert.True(menuItem.IsSubMenuOpen);
    }

    [Fact]
    public void Invoke_Raises_Click()
    {
        var menuItem = new MenuItem();
        var provider = GetInvokeProvider(menuItem);
        var clicked = 0;

        menuItem.Click += (_, _) => clicked++;
        provider.Invoke();

        Assert.Equal(1, clicked);
    }

    [Fact]
    public void Invoke_Executes_Command()
    {
        var executed = 0;
        var menuItem = new MenuItem
        {
            Command = new TestCommand(p => executed += (int)p!),
            CommandParameter = 5,
        };
        var provider = GetInvokeProvider(menuItem);

        provider.Invoke();

        Assert.Equal(5, executed);
    }

    [Fact]
    public void Invoke_Throws_When_Disabled()
    {
        var menuItem = new MenuItem { IsEnabled = false };
        var provider = GetInvokeProvider(menuItem);
        var clicked = 0;

        menuItem.Click += (_, _) => clicked++;

        Assert.Throws<ElementNotEnabledException>(() => provider.Invoke());
        Assert.Equal(0, clicked);
    }

    [Fact]
    public void Invoke_Throws_When_Command_Cannot_Execute()
    {
        var executed = 0;
        var menuItem = new MenuItem { Command = new TestCommand(_ => executed++, canExecute: false) };
        var provider = GetInvokeProvider(menuItem);

        Assert.Throws<ElementNotEnabledException>(() => provider.Invoke());
        Assert.Equal(0, executed);
    }

    [Fact]
    public void Expand_Opens_Top_Level_Menu_Item()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);

        var child = new MenuItem { Header = "Child" };
        var topLevel = new MenuItem { Header = "Top", Items = { child } };
        var menu = new Menu { Items = { topLevel } };
        CreateWindow(menu);

        GetExpandCollapseProvider(topLevel).Expand();

        Assert.True(menu.IsOpen);
        Assert.True(topLevel.IsSubMenuOpen);
        Assert.True(child.IsAttachedToVisualTree);
    }

    [Fact]
    public void Collapse_Closes_Menu_For_Top_Level_Menu_Item()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);

        var topLevel = new MenuItem { Header = "Top", Items = { new MenuItem { Header = "Child" } } };
        var menu = new Menu { Items = { topLevel } };
        CreateWindow(menu);
        var provider = GetExpandCollapseProvider(topLevel);

        provider.Expand();
        provider.Collapse();

        Assert.False(topLevel.IsSubMenuOpen);
        Assert.False(menu.IsOpen);
    }

    [Fact]
    public void Collapse_Does_Nothing_For_Collapsed_Top_Level_Menu_Item()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);

        var first = new MenuItem { Header = "First", Items = { new MenuItem { Header = "Child" } } };
        var second = new MenuItem { Header = "Second", Items = { new MenuItem { Header = "Child" } } };
        var menu = new Menu { Items = { first, second } };
        CreateWindow(menu);

        GetExpandCollapseProvider(second).Expand();
        GetExpandCollapseProvider(first).Collapse();

        Assert.True(second.IsSubMenuOpen);
        Assert.True(menu.IsOpen);
    }

    [Fact]
    public void Collapse_Leaves_Menu_Open_For_Nested_Menu_Item()
    {
        // The nested submenu opens a popup from within a popup.
        using var app = UnitTestApplication.Start(TestServices.StyledWindow.With(
            windowingPlatform: new MockWindowingPlatform(popupImpl: CreateNestablePopup)));

        var nested = new MenuItem { Header = "Nested", Items = { new MenuItem { Header = "Child" } } };
        var topLevel = new MenuItem { Header = "Top", Items = { nested } };
        var menu = new Menu { Items = { topLevel } };
        CreateWindow(menu);
        var nestedProvider = GetExpandCollapseProvider(nested);

        GetExpandCollapseProvider(topLevel).Expand();
        nestedProvider.Expand();
        nestedProvider.Collapse();

        Assert.False(nested.IsSubMenuOpen);
        Assert.True(topLevel.IsSubMenuOpen);
        Assert.True(menu.IsOpen);
    }

    [Fact]
    public void Invoke_In_Menu_Bubbles_Click_To_Menu()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);

        var child = new MenuItem { Header = "Child" };
        var topLevel = new MenuItem { Header = "Top", Items = { child } };
        var menu = new Menu { Items = { topLevel } };
        CreateWindow(menu);
        var clicked = new List<object?>();

        menu.AddHandler(MenuItem.ClickEvent, (_, e) => clicked.Add(e.Source));
        GetExpandCollapseProvider(topLevel).Expand();
        GetInvokeProvider(child).Invoke();

        Assert.Equal([child], clicked);
    }

    [Fact]
    public void Invoke_In_Menu_Closes_Menu()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);

        var child = new MenuItem { Header = "Child" };
        var topLevel = new MenuItem { Header = "Top", Items = { child } };
        var menu = new Menu { Items = { topLevel } };
        CreateWindow(menu);

        GetExpandCollapseProvider(topLevel).Expand();
        Assert.True(menu.IsOpen);

        GetInvokeProvider(child).Invoke();

        Assert.False(topLevel.IsSubMenuOpen);
        Assert.False(menu.IsOpen);
    }

    [Fact]
    public void Invoke_In_Menu_Respects_StaysOpenOnClick()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);

        var child = new MenuItem { Header = "Child", StaysOpenOnClick = true };
        var topLevel = new MenuItem { Header = "Top", Items = { child } };
        var menu = new Menu { Items = { topLevel } };
        CreateWindow(menu);

        GetExpandCollapseProvider(topLevel).Expand();
        GetInvokeProvider(child).Invoke();

        Assert.True(topLevel.IsSubMenuOpen);
        Assert.True(menu.IsOpen);
    }

    [Fact]
    public void Invoke_In_Menu_Toggles_CheckBox_Item()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);

        var child = new MenuItem { Header = "Child", ToggleType = MenuItemToggleType.CheckBox };
        var topLevel = new MenuItem { Header = "Top", Items = { child } };
        CreateWindow(new Menu { Items = { topLevel } });

        GetExpandCollapseProvider(topLevel).Expand();
        GetInvokeProvider(child).Invoke();

        Assert.True(child.IsChecked);
    }

    [Fact]
    public void Invoke_In_ContextMenu_Closes_ContextMenu()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);

        var child = new MenuItem { Header = "Child" };
        var contextMenu = new ContextMenu { Items = { child } };
        var window = new Window { ContextMenu = contextMenu };

        window.Show();
        contextMenu.Open();
        Assert.True(contextMenu.IsOpen);

        GetInvokeProvider(child).Invoke();

        Assert.False(contextMenu.IsOpen);
    }

    [Fact]
    public void Invoke_In_MenuFlyout_Closes_Flyout()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);

        var child = new MenuItem { Header = "Child" };
        var flyout = new MenuFlyout { Items = { child } };
        var button = new Button { ContextFlyout = flyout };
        var window = new Window { Content = button };

        window.Show();
        flyout.ShowAt(button);
        Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded, TestContext.Current.CancellationToken);
        Assert.True(flyout.IsOpen);

        GetInvokeProvider(child).Invoke();

        Assert.False(flyout.IsOpen);
    }

    private static MenuItem CreateSubmenuItem() => new() { Items = { new MenuItem() } };

    private static IPopupImpl CreateNestablePopup(IWindowBaseImpl parent)
    {
        var popup = MockWindowingPlatform.CreatePopupMock(parent);
        popup.Setup(x => x.CreatePopup()).Returns(() => CreateNestablePopup(popup.Object));
        return popup.Object;
    }

    private static Window CreateWindow(Menu menu)
    {
        var window = new Window { Content = menu };
        window.Show();
        window.LayoutManager.ExecuteInitialLayoutPass();
        return window;
    }

    private static IExpandCollapseProvider GetExpandCollapseProvider(MenuItem menuItem)
    {
        var provider = ControlAutomationPeer.CreatePeerForElement(menuItem).GetProvider<IExpandCollapseProvider>();
        Assert.NotNull(provider);
        return provider;
    }

    private static IInvokeProvider GetInvokeProvider(MenuItem menuItem)
    {
        var provider = ControlAutomationPeer.CreatePeerForElement(menuItem).GetProvider<IInvokeProvider>();
        Assert.NotNull(provider);
        return provider;
    }

    private static IToggleProvider GetProvider(MenuItem menuItem)
    {
        var provider = ControlAutomationPeer.CreatePeerForElement(menuItem).GetProvider<IToggleProvider>();
        Assert.NotNull(provider);
        return provider;
    }

    private class TestCommand(Action<object?> execute, bool canExecute = true) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => canExecute;
        public void Execute(object? parameter) => execute(parameter);
    }
}
