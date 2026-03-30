using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class AppBarButtonTests : ScopedTestBase
{
    [Fact]
    public void Label_DefaultIsNull()
        => Assert.Null(new AppBarButton().Label);

    [Fact]
    public void Label_RoundTrip()
    {
        var btn = new AppBarButton { Label = "Save" };
        Assert.Equal("Save", btn.Label);
    }

    [Fact]
    public void Icon_DefaultIsNull()
        => Assert.Null(new AppBarButton().Icon);

    [Fact]
    public void Icon_RoundTrip()
    {
        var btn = new AppBarButton();
        var icon = new object();
        btn.Icon = icon;
        Assert.Same(icon, btn.Icon);
    }

    [Fact]
    public void IsCompact_DefaultIsFalse()
        => Assert.False(new AppBarButton().IsCompact);

    [Fact]
    public void IsCompact_RoundTrip()
    {
        var btn = new AppBarButton { IsCompact = true };
        Assert.True(btn.IsCompact);
    }

    [Fact]
    public void DynamicOverflowOrder_DefaultIsZero()
        => Assert.Equal(0, new AppBarButton().DynamicOverflowOrder);

    [Fact]
    public void DynamicOverflowOrder_RoundTrip()
    {
        var btn = new AppBarButton { DynamicOverflowOrder = 3 };
        Assert.Equal(3, btn.DynamicOverflowOrder);
    }

    [Fact]
    public void LabelPosition_DefaultIsBottom()
        => Assert.Equal(CommandBarDefaultLabelPosition.Bottom, new AppBarButton().LabelPosition);

    [Fact]
    public void LabelPosition_RoundTrip()
    {
        var btn = new AppBarButton { LabelPosition = CommandBarDefaultLabelPosition.Right };
        Assert.Equal(CommandBarDefaultLabelPosition.Right, btn.LabelPosition);
    }

    [Fact]
    public void IsInOverflow_DefaultIsFalse()
        => Assert.False(new AppBarButton().IsInOverflow);

    [Fact]
    public void IsInOverflow_RoundTrip()
    {
        var btn = new AppBarButton { IsInOverflow = true };
        Assert.True(btn.IsInOverflow);
    }

    [Fact]
    public void ImplementsICommandBarElement()
        => Assert.IsAssignableFrom<ICommandBarElement>(new AppBarButton());

    [Fact]
    public void ICommandBarElement_IsCompact_ReadWrite()
    {
        ICommandBarElement elem = new AppBarButton();
        elem.IsCompact = true;
        Assert.True(elem.IsCompact);
    }

    [Fact]
    public void Command_DefaultIsNull()
        => Assert.Null(new AppBarButton().Command);

    [Fact]
    public void CommandParameter_DefaultIsNull()
        => Assert.Null(new AppBarButton().CommandParameter);

    [Fact]
    public void Command_RoundTrip()
    {
        var btn = new AppBarButton();
        var cmd = new DelegateCommand(_ => { });
        btn.Command = cmd;
        Assert.Same(cmd, btn.Command);
    }

    [Fact]
    public void CommandParameter_RoundTrip()
    {
        var btn = new AppBarButton { CommandParameter = "param" };
        Assert.Equal("param", btn.CommandParameter);
    }
}

public class AppBarToggleButtonTests : ScopedTestBase
{
    [Fact]
    public void Label_DefaultIsNull()
        => Assert.Null(new AppBarToggleButton().Label);

    [Fact]
    public void Label_RoundTrip()
    {
        var btn = new AppBarToggleButton { Label = "Bold" };
        Assert.Equal("Bold", btn.Label);
    }

    [Fact]
    public void Icon_DefaultIsNull()
        => Assert.Null(new AppBarToggleButton().Icon);

    [Fact]
    public void IsCompact_DefaultIsFalse()
        => Assert.False(new AppBarToggleButton().IsCompact);

    [Fact]
    public void IsCompact_RoundTrip()
    {
        var btn = new AppBarToggleButton { IsCompact = true };
        Assert.True(btn.IsCompact);
    }

    [Fact]
    public void DynamicOverflowOrder_DefaultIsZero()
        => Assert.Equal(0, new AppBarToggleButton().DynamicOverflowOrder);

    [Fact]
    public void DynamicOverflowOrder_RoundTrip()
    {
        var btn = new AppBarToggleButton { DynamicOverflowOrder = 5 };
        Assert.Equal(5, btn.DynamicOverflowOrder);
    }

    [Fact]
    public void LabelPosition_DefaultIsBottom()
        => Assert.Equal(CommandBarDefaultLabelPosition.Bottom, new AppBarToggleButton().LabelPosition);

    [Fact]
    public void LabelPosition_RoundTrip()
    {
        var btn = new AppBarToggleButton { LabelPosition = CommandBarDefaultLabelPosition.Collapsed };
        Assert.Equal(CommandBarDefaultLabelPosition.Collapsed, btn.LabelPosition);
    }

    [Fact]
    public void IsInOverflow_DefaultIsFalse()
        => Assert.False(new AppBarToggleButton().IsInOverflow);

    [Fact]
    public void ImplementsICommandBarElement()
        => Assert.IsAssignableFrom<ICommandBarElement>(new AppBarToggleButton());

    [Fact]
    public void ICommandBarElement_IsCompact_ReadWrite()
    {
        ICommandBarElement elem = new AppBarToggleButton();
        elem.IsCompact = true;
        Assert.True(elem.IsCompact);
    }

    [Fact]
    public void Command_DefaultIsNull()
        => Assert.Null(new AppBarToggleButton().Command);

    [Fact]
    public void Command_RoundTrip()
    {
        var btn = new AppBarToggleButton();
        var cmd = new DelegateCommand(_ => { });
        btn.Command = cmd;
        Assert.Same(cmd, btn.Command);
    }

    [Fact]
    public void CommandParameter_RoundTrip()
    {
        var btn = new AppBarToggleButton { CommandParameter = 42 };
        Assert.Equal(42, btn.CommandParameter);
    }
}

public class AppBarSeparatorTests : ScopedTestBase
{
    [Fact]
    public void IsCompact_DefaultIsFalse()
        => Assert.False(new AppBarSeparator().IsCompact);

    [Fact]
    public void IsCompact_RoundTrip()
    {
        var sep = new AppBarSeparator { IsCompact = true };
        Assert.True(sep.IsCompact);
    }

    [Fact]
    public void IsInOverflow_DefaultIsFalse()
        => Assert.False(new AppBarSeparator().IsInOverflow);

    [Fact]
    public void IsInOverflow_RoundTrip()
    {
        var sep = new AppBarSeparator { IsInOverflow = true };
        Assert.True(sep.IsInOverflow);
    }

    [Fact]
    public void ImplementsICommandBarElement()
        => Assert.IsAssignableFrom<ICommandBarElement>(new AppBarSeparator());

    [Fact]
    public void ICommandBarElement_IsCompact_ReadWrite()
    {
        ICommandBarElement elem = new AppBarSeparator();
        elem.IsCompact = true;
        Assert.True(elem.IsCompact);
    }
}

public class CommandBarEnumTests : ScopedTestBase
{
    [Fact]
    public void LabelPosition_Bottom_IsZero()
        => Assert.Equal(0, (int)CommandBarDefaultLabelPosition.Bottom);

    [Fact]
    public void LabelPosition_Right_IsOne()
        => Assert.Equal(1, (int)CommandBarDefaultLabelPosition.Right);

    [Fact]
    public void LabelPosition_Collapsed_IsTwo()
        => Assert.Equal(2, (int)CommandBarDefaultLabelPosition.Collapsed);

    [Fact]
    public void OverflowButtonVisibility_Auto_IsZero()
        => Assert.Equal(0, (int)CommandBarOverflowButtonVisibility.Auto);

    [Fact]
    public void OverflowButtonVisibility_Visible_IsOne()
        => Assert.Equal(1, (int)CommandBarOverflowButtonVisibility.Visible);

    [Fact]
    public void OverflowButtonVisibility_Collapsed_IsTwo()
        => Assert.Equal(2, (int)CommandBarOverflowButtonVisibility.Collapsed);
}

public class CommandBarDefaultsTests : ScopedTestBase
{
    [Fact]
    public void DefaultLabelPosition_IsBottom()
        => Assert.Equal(CommandBarDefaultLabelPosition.Bottom, new CommandBar().DefaultLabelPosition);

    [Fact]
    public void OverflowButtonVisibility_DefaultIsAuto()
        => Assert.Equal(CommandBarOverflowButtonVisibility.Auto, new CommandBar().OverflowButtonVisibility);

    [Fact]
    public void IsOpen_DefaultIsFalse()
        => Assert.False(new CommandBar().IsOpen);

    [Fact]
    public void IsSticky_DefaultIsFalse()
        => Assert.False(new CommandBar().IsSticky);

    [Fact]
    public void IsDynamicOverflowEnabled_DefaultIsFalse()
        => Assert.False(new CommandBar().IsDynamicOverflowEnabled);

    [Fact]
    public void HasSecondaryCommands_DefaultIsFalse()
        => Assert.False(new CommandBar().HasSecondaryCommands);

    [Fact]
    public void IsOverflowButtonVisible_DefaultIsFalse()
        => Assert.False(new CommandBar().IsOverflowButtonVisible);

    [Fact]
    public void Content_DefaultIsNull()
        => Assert.Null(new CommandBar().Content);

    [Fact]
    public void PrimaryCommands_NotNull()
        => Assert.NotNull(new CommandBar().PrimaryCommands);

    [Fact]
    public void SecondaryCommands_NotNull()
        => Assert.NotNull(new CommandBar().SecondaryCommands);

    [Fact]
    public void VisiblePrimaryCommands_NotNull()
        => Assert.NotNull(new CommandBar().VisiblePrimaryCommands);

    [Fact]
    public void OverflowItems_NotNull()
        => Assert.NotNull(new CommandBar().OverflowItems);

    [Fact]
    public void PrimaryCommands_StartsEmpty()
        => Assert.Empty(new CommandBar().PrimaryCommands);

    [Fact]
    public void SecondaryCommands_StartsEmpty()
        => Assert.Empty(new CommandBar().SecondaryCommands);

    [Fact]
    public void PrimaryCommands_ReturnsNewListWhenNull()
    {
        var cb = new CommandBar();
        cb.ClearValue(CommandBar.PrimaryCommandsProperty);
        Assert.NotNull(cb.PrimaryCommands);
        Assert.Empty(cb.PrimaryCommands);
    }

    [Fact]
    public void SecondaryCommands_ReturnsNewListWhenNull()
    {
        var cb = new CommandBar();
        cb.ClearValue(CommandBar.SecondaryCommandsProperty);
        Assert.NotNull(cb.SecondaryCommands);
        Assert.Empty(cb.SecondaryCommands);
    }

    [Fact]
    public void VisiblePrimaryCommands_StartsEmpty()
        => Assert.Empty(new CommandBar().VisiblePrimaryCommands);

    [Fact]
    public void OverflowItems_StartsEmpty()
        => Assert.Empty(new CommandBar().OverflowItems);

    [Fact]
    public void ItemWidthBottom_DefaultIs70()
        => Assert.Equal(70d, new CommandBar().ItemWidthBottom);

    [Fact]
    public void ItemWidthRight_DefaultIs102()
        => Assert.Equal(102d, new CommandBar().ItemWidthRight);

    [Fact]
    public void ItemWidthCollapsed_DefaultIs42()
        => Assert.Equal(42d, new CommandBar().ItemWidthCollapsed);
}

public class CommandBarPropertyRoundTripTests : ScopedTestBase
{
    [Fact]
    public void Content_RoundTrip()
    {
        var cb = new CommandBar();
        var content = new object();
        cb.Content = content;
        Assert.Same(content, cb.Content);
    }

    [Fact]
    public void DefaultLabelPosition_RoundTrip()
    {
        var cb = new CommandBar { DefaultLabelPosition = CommandBarDefaultLabelPosition.Right };
        Assert.Equal(CommandBarDefaultLabelPosition.Right, cb.DefaultLabelPosition);
    }

    [Fact]
    public void IsOpen_RoundTrip()
    {
        var cb = new CommandBar { IsOpen = true };
        Assert.True(cb.IsOpen);
        cb.IsOpen = false;
        Assert.False(cb.IsOpen);
    }

    [Fact]
    public void IsSticky_RoundTrip()
    {
        var cb = new CommandBar { IsSticky = true };
        Assert.True(cb.IsSticky);
    }

    [Fact]
    public void IsDynamicOverflowEnabled_RoundTrip()
    {
        var cb = new CommandBar { IsDynamicOverflowEnabled = true };
        Assert.True(cb.IsDynamicOverflowEnabled);
    }

    [Fact]
    public void OverflowButtonVisibility_RoundTrip()
    {
        var cb = new CommandBar { OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Visible };
        Assert.Equal(CommandBarOverflowButtonVisibility.Visible, cb.OverflowButtonVisibility);
    }

    [Fact]
    public void ItemWidthBottom_RoundTrip()
    {
        var cb = new CommandBar { ItemWidthBottom = 80d };
        Assert.Equal(80d, cb.ItemWidthBottom);
    }

    [Fact]
    public void ItemWidthRight_RoundTrip()
    {
        var cb = new CommandBar { ItemWidthRight = 120d };
        Assert.Equal(120d, cb.ItemWidthRight);
    }

    [Fact]
    public void ItemWidthCollapsed_RoundTrip()
    {
        var cb = new CommandBar { ItemWidthCollapsed = 50d };
        Assert.Equal(50d, cb.ItemWidthCollapsed);
    }
}

public class CommandBarIsOpenTests : ScopedTestBase
{
    [Fact]
    public void Opening_FiredWhenIsOpenBecomesTrue()
    {
        var cb = new CommandBar();
        bool fired = false;
        cb.Opening += (_, _) => fired = true;
        cb.IsOpen = true;
        Assert.True(fired);
    }

    [Fact]
    public void Opened_FiredWhenIsOpenBecomesTrue()
    {
        var cb = new CommandBar();
        bool fired = false;
        cb.Opened += (_, _) => fired = true;
        cb.IsOpen = true;
        Assert.True(fired);
    }

    [Fact]
    public void Closing_FiredWhenIsOpenBecomesFalse()
    {
        var cb = new CommandBar { IsOpen = true };
        bool fired = false;
        cb.Closing += (_, _) => fired = true;
        cb.IsOpen = false;
        Assert.True(fired);
    }

    [Fact]
    public void Closed_FiredWhenIsOpenBecomesFalse()
    {
        var cb = new CommandBar { IsOpen = true };
        bool fired = false;
        cb.Closed += (_, _) => fired = true;
        cb.IsOpen = false;
        Assert.True(fired);
    }

    [Fact]
    public void Opening_NotFiredWhenAlreadyOpen()
    {
        var cb = new CommandBar();
        cb.IsOpen = true;
        int count = 0;
        cb.Opening += (_, _) => count++;
        cb.IsOpen = true;
        Assert.Equal(0, count);
    }

    [Fact]
    public void Closing_NotFiredWhenAlreadyClosed()
    {
        var cb = new CommandBar();
        int count = 0;
        cb.Closing += (_, _) => count++;
        cb.IsOpen = false;
        Assert.Equal(0, count);
    }

    [Fact]
    public void Events_FiredInOrder_OpenThenClose()
    {
        var cb = new CommandBar();
        var events = new List<string>();
        cb.Opening += (_, _) => events.Add("Opening");
        cb.Opened  += (_, _) => events.Add("Opened");
        cb.Closing += (_, _) => events.Add("Closing");
        cb.Closed  += (_, _) => events.Add("Closed");

        cb.IsOpen = true;
        cb.IsOpen = false;

        Assert.Equal(new[] { "Opening", "Opened", "Closing", "Closed" }, events);
    }
}

public class CommandBarCollectionTests : ScopedTestBase
{
    [Fact]
    public void PrimaryCommands_Added_AppearInVisiblePrimary_WhenDynamicOverflowDisabled()
    {
        var cb = new CommandBar();
        var btn = new AppBarButton { Label = "Save" };
        cb.PrimaryCommands!.Add(btn);
        Assert.Contains(btn, cb.VisiblePrimaryCommands);
    }

    [Fact]
    public void PrimaryCommands_DefaultCollection_DoesNotDuplicateVisiblePrimaryNotifications()
    {
        var cb = new CommandBar();
        var notifications = 0;

        ((INotifyCollectionChanged)cb.VisiblePrimaryCommands).CollectionChanged += (_, _) => notifications++;

        cb.PrimaryCommands!.Add(new AppBarButton { Label = "Save" });

        Assert.Equal(2, notifications);
    }

    [Fact]
    public void PrimaryCommands_Removed_DisappearsFromVisiblePrimary()
    {
        var cb = new CommandBar();
        var btn = new AppBarButton { Label = "Save" };
        cb.PrimaryCommands!.Add(btn);
        cb.PrimaryCommands!.Remove(btn);
        Assert.DoesNotContain(btn, cb.VisiblePrimaryCommands);
    }

    [Fact]
    public void SecondaryCommands_Added_AppearInOverflowItems()
    {
        var cb = new CommandBar();
        var btn = new AppBarButton { Label = "Settings" };
        cb.SecondaryCommands!.Add(btn);
        Assert.Contains(btn, cb.OverflowItems);
    }

    [Fact]
    public void SecondaryCommands_DefaultCollection_DoesNotDuplicateOverflowNotifications()
    {
        var cb = new CommandBar();
        var notifications = 0;

        ((INotifyCollectionChanged)cb.OverflowItems).CollectionChanged += (_, _) => notifications++;

        cb.SecondaryCommands!.Add(new AppBarButton { Label = "Settings" });

        Assert.Equal(2, notifications);
    }

    [Fact]
    public void SecondaryCommands_Removed_DisappearsFromOverflowItems()
    {
        var cb = new CommandBar();
        var btn = new AppBarButton { Label = "Settings" };
        cb.SecondaryCommands!.Add(btn);
        cb.SecondaryCommands!.Remove(btn);
        Assert.DoesNotContain(btn, cb.OverflowItems);
    }

    [Fact]
    public void HasSecondaryCommands_TrueWhenSecondaryAdded()
    {
        var cb = new CommandBar();
        cb.SecondaryCommands!.Add(new AppBarButton { Label = "Options" });
        Assert.True(cb.HasSecondaryCommands);
    }

    [Fact]
    public void HasSecondaryCommands_FalseAfterSecondaryCleared()
    {
        var cb = new CommandBar();
        var btn = new AppBarButton { Label = "Options" };
        cb.SecondaryCommands!.Add(btn);
        cb.SecondaryCommands!.Remove(btn);
        Assert.False(cb.HasSecondaryCommands);
    }

    [Fact]
    public void OverflowItems_CountMatchesSecondaryCommandCount()
    {
        var cb = new CommandBar();
        cb.SecondaryCommands!.Add(new AppBarButton());
        cb.SecondaryCommands!.Add(new AppBarButton());
        Assert.Equal(2, cb.OverflowItems.Count);
    }

    [Fact]
    public void VisiblePrimaryCommands_CountMatchesPrimary_WhenDynamicOverflowDisabled()
    {
        var cb = new CommandBar();
        cb.PrimaryCommands!.Add(new AppBarButton());
        cb.PrimaryCommands!.Add(new AppBarButton());
        Assert.Equal(2, cb.VisiblePrimaryCommands.Count);
    }

    [Fact]
    public void MultiplePrimaryCommands_AllVisibleInOrder()
    {
        var cb = new CommandBar();
        var btn1 = new AppBarButton { Label = "A" };
        var btn2 = new AppBarButton { Label = "B" };
        var btn3 = new AppBarButton { Label = "C" };
        cb.PrimaryCommands!.Add(btn1);
        cb.PrimaryCommands!.Add(btn2);
        cb.PrimaryCommands!.Add(btn3);
        Assert.Equal(new ICommandBarElement[] { btn1, btn2, btn3 }, cb.VisiblePrimaryCommands);
    }

    [Fact]
    public void AppBarSeparator_CanBeAddedToPrimaryCommands()
    {
        var cb = new CommandBar();
        var sep = new AppBarSeparator();
        cb.PrimaryCommands!.Add(sep);
        Assert.Contains(sep, cb.VisiblePrimaryCommands);
    }

    [Fact]
    public void AppBarToggleButton_CanBeAddedToPrimaryCommands()
    {
        var cb = new CommandBar();
        var toggle = new AppBarToggleButton { Label = "Bold" };
        cb.PrimaryCommands!.Add(toggle);
        Assert.Contains(toggle, cb.VisiblePrimaryCommands);
    }
}

public class CommandBarLabelPositionTests : ScopedTestBase
{
    [Fact]
    public void DefaultLabelPosition_Collapsed_SetsIsCompactOnExistingPrimaryButton()
    {
        var cb = new CommandBar();
        var btn = new AppBarButton();
        cb.PrimaryCommands!.Add(btn);

        cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed;

        Assert.True(btn.IsCompact);
    }

    [Fact]
    public void DefaultLabelPosition_Bottom_ClearsIsCompactOnPrimaryButton()
    {
        var cb = new CommandBar();
        var btn = new AppBarButton();
        cb.PrimaryCommands!.Add(btn);
        cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed;

        cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Bottom;

        Assert.False(btn.IsCompact);
    }

    [Fact]
    public void DefaultLabelPosition_Right_SetsLabelPositionOnPrimaryButton()
    {
        var cb = new CommandBar();
        var btn = new AppBarButton();
        cb.PrimaryCommands!.Add(btn);

        cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Right;

        Assert.Equal(CommandBarDefaultLabelPosition.Right, btn.LabelPosition);
    }

    [Fact]
    public void DefaultLabelPosition_Collapsed_SetsLabelPositionOnPrimaryButton()
    {
        var cb = new CommandBar();
        var btn = new AppBarButton();
        cb.PrimaryCommands!.Add(btn);

        cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed;

        Assert.Equal(CommandBarDefaultLabelPosition.Collapsed, btn.LabelPosition);
    }

    [Fact]
    public void DefaultLabelPosition_Collapsed_PropagatesIsCompactToToggleButton()
    {
        var cb = new CommandBar();
        var toggle = new AppBarToggleButton();
        cb.PrimaryCommands!.Add(toggle);

        cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed;

        Assert.True(toggle.IsCompact);
        Assert.Equal(CommandBarDefaultLabelPosition.Collapsed, toggle.LabelPosition);
    }

    [Fact]
    public void DefaultLabelPosition_Right_PropagatesLabelPositionToToggleButton()
    {
        var cb = new CommandBar();
        var toggle = new AppBarToggleButton();
        cb.PrimaryCommands!.Add(toggle);

        cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Right;

        Assert.Equal(CommandBarDefaultLabelPosition.Right, toggle.LabelPosition);
    }

    [Fact]
    public void DefaultLabelPosition_Collapsed_SetsIsCompactOnSeparator()
    {
        var cb = new CommandBar();
        var sep = new AppBarSeparator();
        cb.PrimaryCommands!.Add(sep);

        cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed;

        Assert.True(sep.IsCompact);
    }

    [Fact]
    public void NewPrimaryCommand_GetsCurrentLabelPosition_WhenAlreadyCollapsed()
    {
        var cb = new CommandBar { DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed };

        var btn = new AppBarButton();
        cb.PrimaryCommands!.Add(btn);

        Assert.True(btn.IsCompact);
        Assert.Equal(CommandBarDefaultLabelPosition.Collapsed, btn.LabelPosition);
    }

    [Fact]
    public void NewPrimaryCommand_GetsCurrentLabelPosition_WhenRight()
    {
        var cb = new CommandBar { DefaultLabelPosition = CommandBarDefaultLabelPosition.Right };

        var btn = new AppBarButton();
        cb.PrimaryCommands!.Add(btn);

        Assert.Equal(CommandBarDefaultLabelPosition.Right, btn.LabelPosition);
    }

    [Fact]
    public void DefaultLabelPosition_Collapsed_AppliesToSecondaryCommands()
    {
        var cb = new CommandBar();
        var btn = new AppBarButton();
        cb.SecondaryCommands!.Add(btn);

        cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed;

        Assert.True(btn.IsCompact);
    }

    [Fact]
    public void DefaultLabelPosition_DoesNotClearLabelText()
    {
        var cb = new CommandBar();
        var btn = new AppBarButton { Label = "Save" };
        cb.PrimaryCommands!.Add(btn);

        cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed;

        Assert.Equal("Save", btn.Label);
    }
}

public class CommandBarOverflowButtonTests : ScopedTestBase
{
    [Fact]
    public void OverflowButtonVisibility_Visible_SetsIsOverflowButtonVisibleTrue()
    {
        var cb = new CommandBar { OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Visible };
        Assert.True(cb.IsOverflowButtonVisible);
    }

    [Fact]
    public void OverflowButtonVisibility_Collapsed_SetsIsOverflowButtonVisibleFalse()
    {
        var cb = new CommandBar { OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Collapsed };
        Assert.False(cb.IsOverflowButtonVisible);
    }

    [Fact]
    public void OverflowButtonVisibility_Auto_TrueWhenHasSecondaryCommands()
    {
        var cb = new CommandBar();
        cb.SecondaryCommands!.Add(new AppBarButton());
        Assert.True(cb.IsOverflowButtonVisible);
    }

    [Fact]
    public void OverflowButtonVisibility_Auto_FalseWhenNoSecondaryCommands()
    {
        var cb = new CommandBar();
        Assert.False(cb.IsOverflowButtonVisible);
    }

    [Fact]
    public void OverflowButtonVisibility_Visible_RemainsTrueWithoutSecondary()
    {
        var cb = new CommandBar { OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Visible };
        Assert.True(cb.IsOverflowButtonVisible);
    }

    [Fact]
    public void OverflowButtonVisibility_Collapsed_RemainsFalseEvenWithSecondary()
    {
        var cb = new CommandBar { OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Collapsed };
        cb.SecondaryCommands!.Add(new AppBarButton());
        Assert.False(cb.IsOverflowButtonVisible);
    }

    [Fact]
    public void OverflowButtonVisibility_Auto_FalseAfterSecondaryRemoved()
    {
        var cb = new CommandBar();
        var btn = new AppBarButton();
        cb.SecondaryCommands!.Add(btn);
        Assert.True(cb.IsOverflowButtonVisible);

        cb.SecondaryCommands!.Remove(btn);
        Assert.False(cb.IsOverflowButtonVisible);
    }

    [Fact]
    public void OverflowButtonVisibility_SwitchFromAutoToVisible_ShowsButtonImmediately()
    {
        var cb = new CommandBar();
        Assert.False(cb.IsOverflowButtonVisible);

        cb.OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Visible;
        Assert.True(cb.IsOverflowButtonVisible);
    }
}

public class CommandBarItemWidthTests : ScopedTestBase
{
    private static CommandBar CreateWithWidth(double width)
    {
        var cb = new CommandBar();
        cb.Measure(new Size(width, double.PositiveInfinity));
        return cb;
    }

    [Fact]
    public void ItemWidthBottom_Controls_HowManyButtonsFit()
    {
        var cb = CreateWithWidth(300);
        var secondary = new AppBarButton();
        cb.SecondaryCommands!.Add(secondary); // forces overflow button
        for (int i = 0; i < 4; i++)
            cb.PrimaryCommands!.Add(new AppBarButton());
        cb.IsDynamicOverflowEnabled = true;

        Assert.Equal(3, cb.VisiblePrimaryCommands.Count);
        Assert.Equal(3, cb.OverflowItems.Count);
        Assert.IsType<AppBarButton>(cb.OverflowItems[0]);
        Assert.IsType<AppBarSeparator>(cb.OverflowItems[1]);
        Assert.Same(secondary, cb.OverflowItems[2]);
    }

    [Fact]
    public void ItemWidthBottom_Reduced_AllowsMoreItemsToFit()
    {
        var cb = CreateWithWidth(300);
        cb.ItemWidthBottom = 35;
        cb.SecondaryCommands!.Add(new AppBarButton());
        for (int i = 0; i < 4; i++)
            cb.PrimaryCommands!.Add(new AppBarButton());
        cb.IsDynamicOverflowEnabled = true;

        Assert.Equal(4, cb.VisiblePrimaryCommands.Count);
    }

    [Fact]
    public void ItemWidthBottom_Large_ReducesToMinimumOneVisible()
    {
        var cb = CreateWithWidth(300);
        cb.ItemWidthBottom = 260;
        cb.SecondaryCommands!.Add(new AppBarButton());
        for (int i = 0; i < 3; i++)
            cb.PrimaryCommands!.Add(new AppBarButton());
        cb.IsDynamicOverflowEnabled = true;

        Assert.Equal(1, cb.VisiblePrimaryCommands.Count);
    }

    [Fact]
    public void ItemWidthRight_UsedWhenLabelPositionIsRight()
    {
        var cb = CreateWithWidth(300);
        cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Right;
        cb.SecondaryCommands!.Add(new AppBarButton());
        for (int i = 0; i < 4; i++)
            cb.PrimaryCommands!.Add(new AppBarButton());
        cb.IsDynamicOverflowEnabled = true;

        Assert.Equal(2, cb.VisiblePrimaryCommands.Count);
    }

    [Fact]
    public void ItemWidthRight_Increased_FewerItemsFit()
    {
        var cb = CreateWithWidth(300);
        cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Right;
        cb.ItemWidthRight = 252; // exactly 1 fits: 252/252=1
        cb.SecondaryCommands!.Add(new AppBarButton());
        for (int i = 0; i < 3; i++)
            cb.PrimaryCommands!.Add(new AppBarButton());
        cb.IsDynamicOverflowEnabled = true;

        Assert.Equal(1, cb.VisiblePrimaryCommands.Count);
    }

    [Fact]
    public void ItemWidthCollapsed_UsedWhenLabelPositionIsCollapsed()
    {
        var cb = CreateWithWidth(300);
        cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed;
        cb.SecondaryCommands!.Add(new AppBarButton());
        for (int i = 0; i < 4; i++)
            cb.PrimaryCommands!.Add(new AppBarButton());
        cb.IsDynamicOverflowEnabled = true;

        Assert.Equal(4, cb.VisiblePrimaryCommands.Count);
    }

    [Fact]
    public void ItemWidths_AreIndependent_PerLabelPosition()
    {
        var cb = CreateWithWidth(300);
        cb.ItemWidthBottom    = 70;
        cb.ItemWidthRight     = 102;
        cb.ItemWidthCollapsed = 42;
        cb.SecondaryCommands!.Add(new AppBarButton());
        for (int i = 0; i < 4; i++)
            cb.PrimaryCommands!.Add(new AppBarButton());

        cb.DefaultLabelPosition    = CommandBarDefaultLabelPosition.Bottom;
        cb.IsDynamicOverflowEnabled = true;
        int visibleBottom = cb.VisiblePrimaryCommands.Count; // 252/70 = 3

        cb.IsDynamicOverflowEnabled = false;
        cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Right;
        cb.IsDynamicOverflowEnabled = true;
        int visibleRight = cb.VisiblePrimaryCommands.Count; // 252/102 = 2

        cb.IsDynamicOverflowEnabled = false;
        cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed;
        cb.IsDynamicOverflowEnabled = true;
        int visibleCollapsed = cb.VisiblePrimaryCommands.Count; // 252/42 = 6 → capped at 4

        Assert.Equal(3, visibleBottom);
        Assert.Equal(2, visibleRight);
        Assert.Equal(4, visibleCollapsed);
    }
}

public class CommandBarSeparatorOverflowTests : ScopedTestBase
{
    private static CommandBar CreateWithWidth(double width)
    {
        var cb = new CommandBar();
        cb.Measure(new Size(width, double.PositiveInfinity));
        return cb;
    }

    [Fact]
    public void TrailingSeparator_IsNotLastVisibleItem()
    {
        // [Btn, Btn, Sep, Btn] with room for 2 buttons: Sep should NOT trail.
        var cb = CreateWithWidth(300);
        cb.SecondaryCommands!.Add(new AppBarButton());
        cb.PrimaryCommands!.Add(new AppBarButton());
        cb.PrimaryCommands.Add(new AppBarButton());
        cb.PrimaryCommands.Add(new AppBarSeparator());
        cb.PrimaryCommands.Add(new AppBarButton());
        cb.IsDynamicOverflowEnabled = true;

        Assert.IsNotType<AppBarSeparator>(cb.VisiblePrimaryCommands[^1]);
    }

    [Fact]
    public void TrailingSeparator_MovedToOverflow()
    {
        // [Btn, Sep, Btn, Btn] with room for 1 button: Sep after the single visible button should overflow.
        var cb = CreateWithWidth(300);
        cb.ItemWidthBottom = 260;
        cb.SecondaryCommands!.Add(new AppBarButton());
        cb.PrimaryCommands!.Add(new AppBarButton());
        cb.PrimaryCommands.Add(new AppBarSeparator());
        cb.PrimaryCommands.Add(new AppBarButton());
        cb.PrimaryCommands.Add(new AppBarButton());
        cb.IsDynamicOverflowEnabled = true;

        Assert.Equal(1, cb.VisiblePrimaryCommands.Count);
        Assert.IsType<AppBarButton>(cb.VisiblePrimaryCommands[0]);
    }

    [Fact]
    public void MultipleSeparators_AllTrailingOnesStripped()
    {
        // [Btn, Sep, Sep, Btn] with room for 1: both trailing separators should be stripped.
        var cb = CreateWithWidth(300);
        cb.ItemWidthBottom = 260;
        cb.SecondaryCommands!.Add(new AppBarButton());
        cb.PrimaryCommands!.Add(new AppBarButton());
        cb.PrimaryCommands.Add(new AppBarSeparator());
        cb.PrimaryCommands.Add(new AppBarSeparator());
        cb.PrimaryCommands.Add(new AppBarButton());
        cb.IsDynamicOverflowEnabled = true;

        Assert.Equal(1, cb.VisiblePrimaryCommands.Count);
        Assert.IsType<AppBarButton>(cb.VisiblePrimaryCommands[0]);
    }

    [Fact]
    public void MidSeparator_StaysVisible_WhenButtonsOnBothSides()
    {
        // [Btn, Sep, Btn] with room for all: separator stays.
        var cb = CreateWithWidth(300);
        cb.PrimaryCommands!.Add(new AppBarButton());
        cb.PrimaryCommands.Add(new AppBarSeparator());
        cb.PrimaryCommands.Add(new AppBarButton());
        cb.IsDynamicOverflowEnabled = true;

        Assert.Equal(3, cb.VisiblePrimaryCommands.Count);
        Assert.IsType<AppBarSeparator>(cb.VisiblePrimaryCommands[1]);
    }

    [Fact]
    public void AllButtonsOverflow_SeparatorsAlsoOverflow()
    {
        // [Sep, Btn, Btn] with room for 0: everything overflows.
        var cb = CreateWithWidth(50);
        cb.SecondaryCommands!.Add(new AppBarButton());
        cb.PrimaryCommands!.Add(new AppBarSeparator());
        cb.PrimaryCommands.Add(new AppBarButton());
        cb.PrimaryCommands.Add(new AppBarButton());
        cb.IsDynamicOverflowEnabled = true;

        Assert.Empty(cb.VisiblePrimaryCommands);
    }

    [Fact]
    public void LeadingSeparator_IsStrippedFromVisible()
    {
        // [Sep, Btn, Btn, Btn] with room for 2: leading Sep should be stripped.
        var cb = CreateWithWidth(300);
        cb.SecondaryCommands!.Add(new AppBarButton());
        cb.PrimaryCommands!.Add(new AppBarSeparator());
        cb.PrimaryCommands.Add(new AppBarButton());
        cb.PrimaryCommands.Add(new AppBarButton());
        cb.PrimaryCommands.Add(new AppBarButton());
        cb.IsDynamicOverflowEnabled = true;

        Assert.IsNotType<AppBarSeparator>(cb.VisiblePrimaryCommands[0]);
    }

    [Fact]
    public void ConsecutiveSeparators_CollapsedToOne()
    {
        // [Btn, Sep, Sep, Btn] all fit: only one separator should remain.
        var cb = CreateWithWidth(300);
        cb.PrimaryCommands!.Add(new AppBarButton());
        cb.PrimaryCommands.Add(new AppBarSeparator());
        cb.PrimaryCommands.Add(new AppBarSeparator());
        cb.PrimaryCommands.Add(new AppBarButton());
        cb.IsDynamicOverflowEnabled = true;

        int sepCount = CountSeparators(cb.VisiblePrimaryCommands);
        Assert.Equal(1, sepCount);
    }

    [Fact]
    public void OrphanedMidSeparator_RemovedWhenNeighborOverflows()
    {
        // [Btn1, Sep, Btn2, Sep, Btn3] with room for 2: Btn3 overflows,
        // second Sep becomes trailing and is removed. First Sep stays.
        var cb = CreateWithWidth(300);
        cb.ItemWidthBottom = 100;
        cb.SecondaryCommands!.Add(new AppBarButton());
        cb.PrimaryCommands!.Add(new AppBarButton());
        cb.PrimaryCommands.Add(new AppBarSeparator());
        cb.PrimaryCommands.Add(new AppBarButton());
        cb.PrimaryCommands.Add(new AppBarSeparator());
        cb.PrimaryCommands.Add(new AppBarButton());
        cb.IsDynamicOverflowEnabled = true;

        Assert.IsNotType<AppBarSeparator>(cb.VisiblePrimaryCommands[^1]);
        Assert.Equal(1, CountSeparators(cb.VisiblePrimaryCommands));
    }

    [Fact]
    public void SeparatorBetweenOverflowedButtons_IsRemoved()
    {
        // [Btn1, Btn2, Sep, Btn3, Btn4] with room for 2: Btn3 and Btn4 overflow,
        // Sep has no non-separator after it in visible set, so it is removed.
        var cb = CreateWithWidth(300);
        cb.ItemWidthBottom = 100;
        cb.SecondaryCommands!.Add(new AppBarButton());
        cb.PrimaryCommands!.Add(new AppBarButton());
        cb.PrimaryCommands.Add(new AppBarButton());
        cb.PrimaryCommands.Add(new AppBarSeparator());
        cb.PrimaryCommands.Add(new AppBarButton());
        cb.PrimaryCommands.Add(new AppBarButton());
        cb.IsDynamicOverflowEnabled = true;

        Assert.Equal(0, CountSeparators(cb.VisiblePrimaryCommands));
    }

    [Fact]
    public void MultipleSeparatorGroups_OnlyValidOnesRemain()
    {
        // [Btn, Sep, Btn, Sep, Btn, Sep, Btn] with room for 3:
        // last Btn overflows, last Sep becomes trailing, the rest stay.
        var cb = CreateWithWidth(300);
        cb.SecondaryCommands!.Add(new AppBarButton());
        cb.PrimaryCommands!.Add(new AppBarButton());
        cb.PrimaryCommands.Add(new AppBarSeparator());
        cb.PrimaryCommands.Add(new AppBarButton());
        cb.PrimaryCommands.Add(new AppBarSeparator());
        cb.PrimaryCommands.Add(new AppBarButton());
        cb.PrimaryCommands.Add(new AppBarSeparator());
        cb.PrimaryCommands.Add(new AppBarButton());
        cb.IsDynamicOverflowEnabled = true;

        Assert.IsNotType<AppBarSeparator>(cb.VisiblePrimaryCommands[^1]);
        Assert.IsNotType<AppBarSeparator>(cb.VisiblePrimaryCommands[0]);
    }

    [Fact]
    public void OnlySeparators_AllOverflow()
    {
        // [Sep, Sep, Sep] with no buttons: all should overflow.
        var cb = CreateWithWidth(300);
        cb.SecondaryCommands!.Add(new AppBarButton());
        cb.PrimaryCommands!.Add(new AppBarSeparator());
        cb.PrimaryCommands.Add(new AppBarSeparator());
        cb.PrimaryCommands.Add(new AppBarSeparator());
        cb.IsDynamicOverflowEnabled = true;

        Assert.Empty(cb.VisiblePrimaryCommands);
    }

    [Fact]
    public void PrimarySeparator_IsRemovedInsteadOfBecomingFirstOverflowItem()
    {
        var cb = CreateWithWidth(300);
        cb.ItemWidthBottom = 260;

        var leadingSeparator = new AppBarSeparator();
        var firstButton = new AppBarButton();
        var overflowedButton = new AppBarButton();

        cb.PrimaryCommands!.Add(leadingSeparator);
        cb.PrimaryCommands.Add(firstButton);
        cb.PrimaryCommands.Add(overflowedButton);
        cb.IsDynamicOverflowEnabled = true;

        Assert.Single(cb.OverflowItems);
        Assert.Same(overflowedButton, cb.OverflowItems[0]);
        Assert.DoesNotContain(leadingSeparator, cb.OverflowItems);
    }

    [Fact]
    public void OverflowedPrimaryCommands_PrecedeSecondaryCommands_WithSyntheticSeparator()
    {
        var cb = CreateWithWidth(300);
        cb.ItemWidthBottom = 260;

        var visiblePrimary = new AppBarButton();
        var originalPrimarySeparator = new AppBarSeparator();
        var overflowedPrimaryOne = new AppBarButton();
        var overflowedPrimaryTwo = new AppBarButton();
        var secondary = new AppBarButton();

        cb.PrimaryCommands!.Add(visiblePrimary);
        cb.PrimaryCommands.Add(originalPrimarySeparator);
        cb.PrimaryCommands.Add(overflowedPrimaryOne);
        cb.PrimaryCommands.Add(overflowedPrimaryTwo);
        cb.SecondaryCommands!.Add(secondary);
        cb.IsDynamicOverflowEnabled = true;

        Assert.Equal(4, cb.OverflowItems.Count);
        Assert.Same(overflowedPrimaryOne, cb.OverflowItems[0]);
        Assert.Same(overflowedPrimaryTwo, cb.OverflowItems[1]);
        Assert.IsType<AppBarSeparator>(cb.OverflowItems[2]);
        Assert.NotSame(originalPrimarySeparator, cb.OverflowItems[2]);
        Assert.Same(secondary, cb.OverflowItems[3]);
        Assert.DoesNotContain(originalPrimarySeparator, cb.OverflowItems);
    }

    [Fact]
    public void HiddenSecondaryCommands_DoNotGetSyntheticOverflowSeparator()
    {
        var cb = CreateWithWidth(300);
        cb.ItemWidthBottom = 260;

        var visiblePrimary = new AppBarButton();
        var overflowedPrimary = new AppBarButton();
        var hiddenSecondary = new AppBarButton { IsVisible = false };

        cb.PrimaryCommands!.Add(visiblePrimary);
        cb.PrimaryCommands.Add(overflowedPrimary);
        cb.SecondaryCommands!.Add(hiddenSecondary);
        cb.IsDynamicOverflowEnabled = true;

        Assert.Equal(2, cb.OverflowItems.Count);
        Assert.Same(overflowedPrimary, cb.OverflowItems[0]);
        Assert.Same(hiddenSecondary, cb.OverflowItems[1]);
        Assert.DoesNotContain(cb.OverflowItems, x => x is AppBarSeparator);
    }

    [Fact]
    public void TogglingSecondaryVisibility_RebuildsSyntheticOverflowSeparator()
    {
        var cb = CreateWithWidth(300);
        cb.ItemWidthBottom = 260;

        var visiblePrimary = new AppBarButton();
        var overflowedPrimary = new AppBarButton();
        var secondary = new AppBarButton();

        cb.PrimaryCommands!.Add(visiblePrimary);
        cb.PrimaryCommands.Add(overflowedPrimary);
        cb.SecondaryCommands!.Add(secondary);
        cb.IsDynamicOverflowEnabled = true;

        Assert.Equal(3, cb.OverflowItems.Count);
        Assert.Same(overflowedPrimary, cb.OverflowItems[0]);
        Assert.IsType<AppBarSeparator>(cb.OverflowItems[1]);
        Assert.Same(secondary, cb.OverflowItems[2]);

        secondary.IsVisible = false;

        Assert.Equal(2, cb.OverflowItems.Count);
        Assert.Same(overflowedPrimary, cb.OverflowItems[0]);
        Assert.Same(secondary, cb.OverflowItems[1]);
        Assert.DoesNotContain(cb.OverflowItems, x => x is AppBarSeparator);

        secondary.IsVisible = true;

        Assert.Equal(3, cb.OverflowItems.Count);
        Assert.Same(overflowedPrimary, cb.OverflowItems[0]);
        Assert.IsType<AppBarSeparator>(cb.OverflowItems[1]);
        Assert.Same(secondary, cb.OverflowItems[2]);
    }

    private static int CountSeparators(IReadOnlyList<ICommandBarElement> items)
    {
        int count = 0;
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i] is AppBarSeparator)
                count++;
        }
        return count;
    }
}

file sealed class DelegateCommand : System.Windows.Input.ICommand
{
    private readonly System.Action<object?> _execute;
    private readonly System.Func<object?, bool> _canExecute;

    public DelegateCommand(System.Action<object?> execute, System.Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute ?? (_ => true);
    }

    public event System.EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? parameter) => _canExecute(parameter);
    public void Execute(object? parameter) => _execute(parameter);
}
