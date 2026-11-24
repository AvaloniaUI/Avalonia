using System;
using Avalonia.Automation;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Avalonia.Automation.Peers
{
    public class AutoCompleteBoxAutomationPeer : ControlAutomationPeer, IExpandCollapseProvider, IValueProvider
    {
        public AutoCompleteBoxAutomationPeer(AutoCompleteBox owner)
            : base(owner)
        {
            owner.PropertyChanged += OwnerPropertyChanged;
        }

        public new AutoCompleteBox Owner => (AutoCompleteBox)base.Owner;

        public ExpandCollapseState ExpandCollapseState => ToState(Owner.IsDropDownOpen);
        public bool ShowsMenu => true;
        public void Collapse() => Owner.SetCurrentValue(AutoCompleteBox.IsDropDownOpenProperty, false);
        public void Expand() => Owner.SetCurrentValue(AutoCompleteBox.IsDropDownOpenProperty, true);
        public bool IsReadOnly => false;

        public string? Value
        {
            get => Owner.Text;
            set
            {
                if (value == Owner.Text)
                    return;

                Owner.SetCurrentValue(AutoCompleteBox.TextProperty, value);
            }
        }

        void IValueProvider.SetValue(string? value) => Owner.SetCurrentValue(AutoCompleteBox.TextProperty, value);

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Group;
        }

        protected override string GetClassNameCore() => nameof(AutoCompleteBox);

        private void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == AutoCompleteBox.IsDropDownOpenProperty)
            {
                RaisePropertyChangedEvent(
                    ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                    ToState(e.GetOldValue<bool>()),
                    ToState(e.GetNewValue<bool>()));
            }
            else if (e.Property == AutoCompleteBox.TextProperty)
            {
                RaisePropertyChangedEvent(
                    ValuePatternIdentifiers.ValueProperty,
                    e.OldValue,
                    e.NewValue);
            }
        }

        private static ExpandCollapseState ToState(bool value)
        {
            return value ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
        }
    }
}
