using System;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.Automation.Peers
{
    public class MenuItemAutomationPeer : ControlAutomationPeer, IToggleProvider
    {
        public MenuItemAutomationPeer(MenuItem owner)
            : base(owner)
        {
            owner.PropertyChanged += OwnerPropertyChanged;
        }

        public new MenuItem Owner => (MenuItem)base.Owner;

        ToggleState IToggleProvider.ToggleState
            => Owner.IsChecked ? ToggleState.On : ToggleState.Off;

        void IToggleProvider.Toggle()
        {
            EnsureEnabled();

            switch (Owner.ToggleType)
            {
                case MenuItemToggleType.CheckBox:
                    Owner.SetCurrentValue(MenuItem.IsCheckedProperty, !Owner.IsChecked);
                    break;
                case MenuItemToggleType.Radio:
                    if (!Owner.IsChecked)
                        Owner.SetCurrentValue(MenuItem.IsCheckedProperty, true);
                    break;
            }
        }

        protected override string? GetAccessKeyCore()
        {
            var result = base.GetAccessKeyCore();

            if (string.IsNullOrWhiteSpace(result))
            {
                if (Owner.HeaderPresenter?.Child is AccessText accessText)
                {
                    result = accessText.AccessKey;
                }
            }

            return result;
        }

        protected override string? GetAcceleratorKeyCore()
        {
            var result = base.GetAcceleratorKeyCore();

            if (string.IsNullOrWhiteSpace(result))
            {
                result = Owner.InputGesture?.ToString();
            }

            return result;
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.MenuItem;
        }

        protected override string? GetNameCore()
        {
            var result = base.GetNameCore();

            if (result is null && Owner.Header is string header)
            {
                result = AccessText.RemoveAccessKeyMarker(header);
            }

            return result;
        }

        protected override object? GetProviderCore(Type providerType)
        {
            if (providerType == typeof(IToggleProvider) && Owner.ToggleType == MenuItemToggleType.None)
                return null;

            return base.GetProviderCore(providerType);
        }

        private void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == MenuItem.IsCheckedProperty && Owner.ToggleType != MenuItemToggleType.None)
            {
                RaisePropertyChangedEvent(
                    TogglePatternIdentifiers.ToggleStateProperty,
                    ToState(e.GetOldValue<bool>()),
                    ToState(e.GetNewValue<bool>()));
            }
        }

        private static ToggleState ToState(bool value)
            => value ? ToggleState.On : ToggleState.Off;
    }
}
