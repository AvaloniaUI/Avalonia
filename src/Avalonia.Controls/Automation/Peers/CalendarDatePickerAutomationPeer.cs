using System;
using Avalonia.Automation;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Avalonia.Automation.Peers
{
    public class CalendarDatePickerAutomationPeer : ControlAutomationPeer,
        IInvokeProvider,
        IExpandCollapseProvider,
        IValueProvider
    {
        public CalendarDatePickerAutomationPeer(CalendarDatePicker owner)
            : base(owner)
        {
            Owner.PropertyChanged += OwnerPropertyChanged;
        }

        public new CalendarDatePicker Owner => (CalendarDatePicker)base.Owner;

        public ExpandCollapseState ExpandCollapseState => ToState(Owner.IsDropDownOpen);

        public bool ShowsMenu => true;

        public bool IsReadOnly => false;

        public string? Value => Owner.Text;

        public void Invoke()
        {
            EnsureEnabled();
            Owner.IsDropDownOpen = true;
        }

        public void Expand() => Owner.IsDropDownOpen = true;

        public void Collapse() => Owner.IsDropDownOpen = false;

        public void SetValue(string? value) => Owner.Text = value;

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

        protected override string GetClassNameCore() => "CalendarDatePicker";

        protected override bool IsContentElementCore() => true;

        protected override bool IsControlElementCore() => true;

        private void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == CalendarDatePicker.TextProperty)
            {
                RaisePropertyChangedEvent(
                    ValuePatternIdentifiers.ValueProperty,
                    e.OldValue,
                    e.NewValue);
            }
            else if (e.Property == CalendarDatePicker.IsDropDownOpenProperty)
            {
                RaisePropertyChangedEvent(
                    ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                    ToState(e.GetOldValue<bool>()),
                    ToState(e.GetNewValue<bool>()));
            }
        }

        private static ExpandCollapseState ToState(bool isExpanded)
        {
            return isExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
        }
    }
}
