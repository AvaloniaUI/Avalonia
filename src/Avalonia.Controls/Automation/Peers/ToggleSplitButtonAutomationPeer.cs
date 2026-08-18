using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Avalonia.Automation.Peers
{
    public class ToggleSplitButtonAutomationPeer : SplitButtonAutomationPeer, IToggleProvider
    {
        public ToggleSplitButtonAutomationPeer(ToggleSplitButton owner)
            : base(owner)
        {
            owner.PropertyChanged += OwnerPropertyChanged;
        }

        public new ToggleSplitButton Owner => (ToggleSplitButton)base.Owner;

        ToggleState IToggleProvider.ToggleState => ToState(Owner.IsChecked);

        void IToggleProvider.Toggle()
        {
            EnsureEnabled();
            Owner.ToggleForAutomation();
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.SplitButton;
        }

        protected override string GetClassNameCore()
        {
            return "ToggleSplitButton";
        }

        private void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ToggleSplitButton.IsCheckedProperty)
            {
                RaisePropertyChangedEvent(
                    TogglePatternIdentifiers.ToggleStateProperty,
                    ToState(e.GetOldValue<bool>()),
                    ToState(e.GetNewValue<bool>()));
            }
        }

        private static ToggleState ToState(bool value)
        {
            return value ? ToggleState.On : ToggleState.Off;
        }
    }
}
