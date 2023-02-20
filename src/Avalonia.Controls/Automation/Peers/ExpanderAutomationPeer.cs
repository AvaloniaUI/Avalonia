using System;
using System.Collections.Generic;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Controls.Automation.Peers
{
    public class ExpanderAutomationPeer : ControlAutomationPeer, IExpandCollapseProvider
    {
        public ExpanderAutomationPeer(Expander owner) : base(owner)
        {
            owner.PropertyChanged += Owner_PropertyChanged;
        }

        protected override string? GetNameCore()
        {
            return base.GetNameCore();
        }
        protected override string GetClassNameCore()
        {
            return "Expander";
        }
        protected override bool IsContentElementCore() => true;
        protected override bool IsControlElementCore() => true;
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Group;
        }

        public ExpandCollapseState ExpandCollapseState => ((Expander)Owner).IsExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

        public bool ShowsMenu => throw new NotImplementedException();

        public void Collapse()
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();

            ((Expander)Owner).IsExpanded = false;
        }

        public void Expand()
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();

            ((Expander)Owner).IsExpanded = true;
        }


        private void Owner_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Expander.IsExpandedProperty)
            {
                RaisePropertyChangedEvent(
                    ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                    (bool)e.OldValue! ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
                    (bool)e.NewValue! ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
            }
        }

    }
}
