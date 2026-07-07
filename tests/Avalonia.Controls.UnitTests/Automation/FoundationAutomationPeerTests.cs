using System;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.UnitTests;
using Xunit;

#nullable enable

namespace Avalonia.Controls.UnitTests.Automation;

public class FoundationAutomationPeerTests
{
    public class ToolTipPeer : ScopedTestBase
    {
        [Fact]
        public void Creates_ToolTipAutomationPeer()
        {
            var control = new ToolTip();
            var peer = ControlAutomationPeer.CreatePeerForElement(control);

            Assert.IsType<ToolTipAutomationPeer>(peer);
        }

        [Fact]
        public void ControlType_Is_ToolTip()
        {
            var control = new ToolTip();
            var peer = (ToolTipAutomationPeer)ControlAutomationPeer.CreatePeerForElement(control);

            Assert.Equal(AutomationControlType.ToolTip, peer.GetAutomationControlType());
            Assert.Equal("ToolTip", peer.GetClassName());
        }
    }

    public class CalendarDatePickerPeer : ScopedTestBase
    {
        [Fact]
        public void Creates_CalendarDatePickerAutomationPeer()
        {
            var control = new CalendarDatePicker();
            var peer = ControlAutomationPeer.CreatePeerForElement(control);

            Assert.IsType<CalendarDatePickerAutomationPeer>(peer);
        }

        [Fact]
        public void Implements_IInvoke_And_IValue_Providers()
        {
            var control = new CalendarDatePicker();
            var peer = ControlAutomationPeer.CreatePeerForElement(control);

            Assert.IsAssignableFrom<IInvokeProvider>(peer);
            Assert.IsAssignableFrom<IExpandCollapseProvider>(peer);
            Assert.IsAssignableFrom<IValueProvider>(peer);
        }

        [Fact]
        public void Has_Button_ControlType()
        {
            var control = new CalendarDatePicker();
            var peer = (CalendarDatePickerAutomationPeer)ControlAutomationPeer.CreatePeerForElement(control);

            Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
            Assert.Equal("CalendarDatePicker", peer.GetClassName());
        }

        [Fact]
        public void Invoke_Opens_DropDown()
        {
            var control = new CalendarDatePicker();
            var peer = (IInvokeProvider)ControlAutomationPeer.CreatePeerForElement(control);

            peer.Invoke();

            Assert.True(control.IsDropDownOpen);
        }

        [Fact]
        public void ExpandCollapse_Tracks_IsDropDownOpen()
        {
            var control = new CalendarDatePicker();
            var peer = (IExpandCollapseProvider)ControlAutomationPeer.CreatePeerForElement(control);

            Assert.True(peer.ShowsMenu);
            Assert.Equal(ExpandCollapseState.Collapsed, peer.ExpandCollapseState);

            peer.Expand();
            Assert.True(control.IsDropDownOpen);
            Assert.Equal(ExpandCollapseState.Expanded, peer.ExpandCollapseState);

            peer.Collapse();
            Assert.False(control.IsDropDownOpen);
            Assert.Equal(ExpandCollapseState.Collapsed, peer.ExpandCollapseState);
        }

        [Fact]
        public void Value_Mirrors_Owner_Text()
        {
            var control = new CalendarDatePicker { Text = "typed value" };
            var peer = (CalendarDatePickerAutomationPeer)ControlAutomationPeer.CreatePeerForElement(control);

            Assert.Equal("typed value", peer.Value);

            control.Text = "updated typed value";

            Assert.Equal("updated typed value", peer.Value);
        }

        [Fact]
        public void SetValue_Updates_Text()
        {
            var control = new CalendarDatePicker();
            var peer = (IValueProvider)ControlAutomationPeer.CreatePeerForElement(control);

            Assert.False(peer.IsReadOnly);

            peer.SetValue("automation text");

            Assert.Equal("automation text", control.Text);
        }

        [Fact]
        public void PropertyChanged_Raises_Value_When_Text_Changes()
        {
            var control = new CalendarDatePicker();
            var peer = (CalendarDatePickerAutomationPeer)ControlAutomationPeer.CreatePeerForElement(control);
            AutomationPropertyChangedEventArgs? changed = null;

            peer.PropertyChanged += (_, e) =>
            {
                if (e.Property == ValuePatternIdentifiers.ValueProperty)
                    changed = e;
            };

            control.Text = "January";

            Assert.NotNull(changed);
            Assert.Equal(ValuePatternIdentifiers.ValueProperty, changed!.Property);
            Assert.Null(changed.OldValue);
            Assert.Equal("January", changed.NewValue);
        }

        [Fact]
        public void PropertyChanged_Raises_ExpandCollapseState_When_DropDown_Open_Changes()
        {
            var control = new CalendarDatePicker();
            var peer = (CalendarDatePickerAutomationPeer)ControlAutomationPeer.CreatePeerForElement(control);
            AutomationPropertyChangedEventArgs? changed = null;

            peer.PropertyChanged += (_, e) =>
            {
                if (e.Property == ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty)
                    changed = e;
            };

            control.IsDropDownOpen = true;

            Assert.NotNull(changed);
            Assert.Equal(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, changed!.Property);
            Assert.Equal(ExpandCollapseState.Collapsed, changed.OldValue);
            Assert.Equal(ExpandCollapseState.Expanded, changed.NewValue);
        }
    }

    public class NumericUpDownPeer : ScopedTestBase
    {
        [Fact]
        public void Creates_NumericUpDownAutomationPeer()
        {
            var control = new NumericUpDown();
            var peer = ControlAutomationPeer.CreatePeerForElement(control);

            Assert.IsType<NumericUpDownAutomationPeer>(peer);
        }

        [Fact]
        public void Is_Spinner_ControlType()
        {
            var control = new NumericUpDown();
            var peer = (NumericUpDownAutomationPeer)ControlAutomationPeer.CreatePeerForElement(control);

            Assert.Equal(AutomationControlType.Spinner, peer.GetAutomationControlType());
            Assert.Equal("NumericUpDown", peer.GetClassName());
        }

        [Fact]
        public void Implements_IRangeValueProvider()
        {
            var control = new NumericUpDown();
            var peer = ControlAutomationPeer.CreatePeerForElement(control);

            Assert.IsAssignableFrom<IRangeValueProvider>(peer);
        }

        [Fact]
        public void Range_Values_Reflect_Owner()
        {
            var control = new NumericUpDown
            {
                Minimum = 10,
                Maximum = 20,
                Increment = 3,
                IsReadOnly = true,
                Value = 14,
            };

            var peer = (IRangeValueProvider)ControlAutomationPeer.CreatePeerForElement(control);

            Assert.Equal(10d, peer.Minimum);
            Assert.Equal(20d, peer.Maximum);
            Assert.Equal(14d, peer.Value);
            Assert.Equal(3d, peer.SmallChange);
            Assert.Equal(3d, peer.LargeChange);
            Assert.True(peer.IsReadOnly);
        }

        [Fact]
        public void Null_Value_Reports_Default_Clamped_To_Range()
        {
            var control = new NumericUpDown
            {
                Minimum = 10,
                Maximum = 20,
                Value = null,
            };

            var peer = (IRangeValueProvider)ControlAutomationPeer.CreatePeerForElement(control);

            Assert.Equal(10d, peer.Value);
        }

        [Fact]
        public void SetValue_Updates_Owner_Value()
        {
            var control = new NumericUpDown();
            var peer = (IRangeValueProvider)ControlAutomationPeer.CreatePeerForElement(control);

            peer.SetValue(42.5);

            Assert.Equal(42.5m, control.Value);
        }

        [Fact]
        public void PropertyChanged_Raises_Range_When_Value_Changes()
        {
            var control = new NumericUpDown();
            var peer = (NumericUpDownAutomationPeer)ControlAutomationPeer.CreatePeerForElement(control);
            AutomationPropertyChangedEventArgs? changed = null;

            peer.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeValuePatternIdentifiers.ValueProperty)
                    changed = e;
            };

            control.Value = 7.5m;

            Assert.NotNull(changed);
            Assert.Equal(RangeValuePatternIdentifiers.ValueProperty, changed!.Property);
            Assert.Null(changed.OldValue);
            Assert.Equal(7.5m, changed.NewValue);
        }
    }
}
