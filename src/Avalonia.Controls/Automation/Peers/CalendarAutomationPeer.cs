using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.Automation.Peers
{
    public class CalendarAutomationPeer : ControlAutomationPeer, ISelectionProvider, IValueProvider
    {
        public CalendarAutomationPeer(Calendar owner)
            : base(owner)
        {
            owner.SelectedDatesChanged += OwnerSelectedDatesChanged;
        }

        public new Calendar Owner => (Calendar)base.Owner;

        public bool CanSelectMultiple =>
            Owner.SelectionMode == CalendarSelectionMode.SingleRange ||
            Owner.SelectionMode == CalendarSelectionMode.MultipleRange;

        public bool IsSelectionRequired => false;

        public IReadOnlyList<AutomationPeer> GetSelection()
        {
            return Owner.SelectedDates.Select(date => Owner.FindDayButtonFromDay(date))
                .OfType<CalendarDayButton>().Select(GetOrCreate).ToList();
        }

        public bool IsReadOnly => true;

        public string? Value => string.Join(
            System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator,
            Owner.SelectedDates.Select(x => x.ToString(System.Globalization.CultureInfo.CurrentCulture)));

        public void SetValue(string? value)
        {
            throw new NotSupportedException();
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Calendar;
        }

        protected override string GetClassNameCore() => nameof(Calendar);

        private void OwnerSelectedDatesChanged(object? sender, SelectionChangedEventArgs e)
        {
            RaisePropertyChangedEvent(SelectionPatternIdentifiers.SelectionProperty, null, null);
            RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, null, null);
        }
    }
}
