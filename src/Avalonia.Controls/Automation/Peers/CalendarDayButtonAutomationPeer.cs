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

            if (TryGetSelectableDate(out var calendar, out var date))
                calendar.SelectedDate = date;
        }

        void ISelectionItemProvider.AddToSelection()
        {
            EnsureEnabled();

            if (!TryGetSelectableDate(out var calendar, out var date) ||
                calendar.SelectedDates.Contains(date))
            {
                return;
            }

            if (calendar.SelectionMode == CalendarSelectionMode.SingleDate)
                calendar.SelectedDate = date;
            else
                calendar.SelectedDates.Add(date);
        }

        void ISelectionItemProvider.RemoveFromSelection()
        {
            EnsureEnabled();

            if (Owner.Owner is { SelectionMode: not CalendarSelectionMode.None } calendar &&
                Owner.DataContext is DateTime date)
            {
                calendar.SelectedDates.Remove(date);
            }
        }

        private bool TryGetSelectableDate(out Calendar calendar, out DateTime date)
        {
            if (Owner.Owner is { SelectionMode: not CalendarSelectionMode.None } owner &&
                Owner.DataContext is DateTime value &&
                !Owner.IsBlackout)
            {
                calendar = owner;
                date = value;
                return true;
            }

            calendar = null!;
            date = default;
            return false;
        }

        protected override string GetClassNameCore() => nameof(CalendarDayButton);
    }
}
