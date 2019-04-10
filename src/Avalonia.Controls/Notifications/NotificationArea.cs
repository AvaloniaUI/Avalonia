using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace Avalonia.Controls.Notifications
{
    public class NotificationArea : TemplatedControl
    {
        private IList _items;

        public NotificationPosition Position
        {
            get { return (NotificationPosition)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        public static readonly StyledProperty<NotificationPosition> PositionProperty =
            AvaloniaProperty.Register<NotificationArea, NotificationPosition>(nameof(Position), NotificationPosition.BottomRight);

        public int MaxItems
        {
            get { return (int)GetValue(MaxItemsProperty); }
            set { SetValue(MaxItemsProperty, value); }
        }

        public static readonly AvaloniaProperty MaxItemsProperty =
            AvaloniaProperty.Register<NotificationArea, int>(nameof(MaxItems), int.MaxValue);

        public NotificationArea()
        {
            NotificationManager.AddArea(this);
        }

        static NotificationArea()
        {
            PseudoClass<NotificationArea, NotificationPosition>(PositionProperty, x => x == NotificationPosition.TopLeft, ":topleft");
            PseudoClass<NotificationArea, NotificationPosition>(PositionProperty, x => x == NotificationPosition.TopRight, ":topright");
            PseudoClass<NotificationArea, NotificationPosition>(PositionProperty, x => x == NotificationPosition.BottomLeft, ":bottomleft");
            PseudoClass<NotificationArea, NotificationPosition>(PositionProperty, x => x == NotificationPosition.BottomRight, ":bottomright");
        }

        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);

            var itemsControl = e.NameScope.Find<Panel>("PART_Items");
            _items = itemsControl?.Children;
        }

        public async void Show(object content, TimeSpan expirationTime, Action onClick, Action onClose)
        {
            var notification = new Notification
            {
                Content = content
            };

            notification.PointerPressed += (sender, args) =>
            {
                if (onClick != null)
                {
                    onClick.Invoke();
                    (sender as Notification)?.Close();
                }
            };
            notification.NotificationClosed += (sender, args) => onClose?.Invoke();
            notification.NotificationClosed += OnNotificationClosed;

            /*if (!this.)
            {
                return;
            }*/

            /*var w = this.VisualRoot Window.GetWindow(this);
            var x = PresentationSource.FromVisual(w);
            if (x == null)
            {
                return;
            }*/

            lock (_items)
            {
                _items.Add(notification);

                if (_items.OfType<Notification>().Count(i => !i.IsClosing) > MaxItems)
                {
                    _items.OfType<Notification>().First(i => !i.IsClosing).Close();
                }
            }

            if (expirationTime == TimeSpan.MaxValue)
            {
                return;
            }
            await Task.Delay(expirationTime);

            notification.Close();
        }

        private void OnNotificationClosed(object sender, RoutedEventArgs routedEventArgs)
        {
            var notification = sender as Notification;
            _items.Remove(notification);
        }
    }

    public enum NotificationPosition
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }
}
