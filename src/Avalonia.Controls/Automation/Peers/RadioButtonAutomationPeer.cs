using System;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Controls.Automation.Peers
{
    public class RadioButtonAutomationPeer : ToggleButtonAutomationPeer, ISelectionItemProvider
    {
        public RadioButtonAutomationPeer(RadioButton owner) : base(owner)
        {
            owner.PropertyChanged += (a, e) =>
            {
                if (e.Property == RadioButton.IsCheckedProperty)
                {
                    RaiseToggleStatePropertyChangedEvent((bool?)e.OldValue, (bool?)e.NewValue);
                }
            };
        }

        override protected string GetClassNameCore()
        {
            return "RadioButton";
        }

        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.RadioButton;
        }

        public bool IsSelected => ((RadioButton)Owner).IsChecked == true;

        public ISelectionProvider? SelectionContainer => null;

        public void AddToSelection()
        {
            if (((RadioButton)Owner).IsChecked != true)
                throw new InvalidOperationException("Operation cannot be performed");
        }

        public void RemoveFromSelection()
        {
            if (((RadioButton)Owner).IsChecked == true)
                throw new InvalidOperationException("Operation cannot be performed");
        }

        public void Select()
        {
            if (!IsEnabled())
                throw new InvalidOperationException("Element is disabled thus it cannot be selected");

            ((RadioButton)Owner).IsChecked = true;
        }

        internal virtual void RaiseToggleStatePropertyChangedEvent(bool? oldValue, bool? newValue)
        {
            RaisePropertyChangedEvent(
                SelectionItemPatternIdentifiers.IsSelectedProperty,
                oldValue == true,
                newValue == true);
        }
    }
}
