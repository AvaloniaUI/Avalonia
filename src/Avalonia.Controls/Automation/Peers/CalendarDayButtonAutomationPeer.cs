using System;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.Automation.Peers
{
    public class CalendarDayButtonAutomationPeer : ButtonAutomationPeer, ISelectionItemProvider
    {
        public CalendarDayButtonAutomationPeer(CalendarDayButton owner)
            : base(owner)
        {
        }

        public new CalendarDayButton Owner => (CalendarDayButton)base.Owner;

        public bool IsSelected => Owner.IsSelected;

        public ISelectionProvider? SelectionContainer
            => Owner.Owner is { } calendar
                ? GetOrCreate(calendar).GetProvider<ISelectionProvider>()
                : null;

        public void Select()
        {
            EnsureEnabled();

            if (Owner.Owner is { } calendar && Owner.DataContext is DateTime date)
                calendar.SelectedDate = date;
        }

        void ISelectionItemProvider.AddToSelection()
        {
            EnsureEnabled();

            if (Owner.Owner is { } calendar && Owner.DataContext is DateTime date &&
                !calendar.SelectedDates.Contains(date))
            {
                calendar.SelectedDates.Add(date);
            }
        }

        void ISelectionItemProvider.RemoveFromSelection()
        {
            EnsureEnabled();

            if (Owner.Owner is { } calendar && Owner.DataContext is DateTime date)
                calendar.SelectedDates.Remove(date);
        }

        protected override string GetClassNameCore() => nameof(CalendarDayButton);
    }
}
