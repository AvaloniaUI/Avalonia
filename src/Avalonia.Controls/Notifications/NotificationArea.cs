using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Notifications
{
    public class NotificationArea : TemplatedControl, INotificationManager
    {
        private IList _items;

        public static NotificationPosition GetPosition(TopLevel obj)
        {
            return obj.GetValue(PositionProperty);
        }

        public static void SetPosition(TopLevel obj, NotificationPosition value)
        {
            obj.SetValue(PositionProperty, value);
        }

        public static readonly AvaloniaProperty<NotificationPosition> PositionProperty =
          AvaloniaProperty.RegisterAttached<NotificationArea, TopLevel, NotificationPosition>("Position", defaultValue: NotificationPosition.TopLeft, inherits: true);

        public NotificationPosition Position
        {
            get { return GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        public static int GetMaxItems(TopLevel obj)
        {
            return obj.GetValue(MaxItemsProperty);
        }

        public static void SetMaxItems(TopLevel obj, int value)
        {
            obj.SetValue(MaxItemsProperty, value);
        }

        public static readonly AvaloniaProperty<int> MaxItemsProperty =
          AvaloniaProperty.RegisterAttached<NotificationArea, TopLevel, int>("Position", defaultValue: 5, inherits: true);

        public int MaxItems
        {
            get { return GetValue(MaxItemsProperty); }
            set { SetValue(MaxItemsProperty, value); }
        }

        static NotificationArea()
        {
            PseudoClass<NotificationArea, NotificationPosition>(PositionProperty, x => x == NotificationPosition.TopLeft, ":topleft");
            PseudoClass<NotificationArea, NotificationPosition>(PositionProperty, x => x == NotificationPosition.TopRight, ":topright");
            PseudoClass<NotificationArea, NotificationPosition>(PositionProperty, x => x == NotificationPosition.BottomLeft, ":bottomleft");
            PseudoClass<NotificationArea, NotificationPosition>(PositionProperty, x => x == NotificationPosition.BottomRight, ":bottomright");

            HorizontalAlignmentProperty.OverrideDefaultValue<NotificationArea>(Layout.HorizontalAlignment.Stretch);
            VerticalAlignmentProperty.OverrideDefaultValue<NotificationArea>(Layout.VerticalAlignment.Stretch);
        }

        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);

            var itemsControl = e.NameScope.Find<Panel>("PART_Items");
            _items = itemsControl?.Children;
        }

        public void Show(NotificationContent content, TimeSpan? expirationTime, Action onClick, Action onClose)
        {
            Show(content as object, expirationTime, onClick, onClose);
        }

        public async void Show(object content, TimeSpan? expirationTime, Action onClick, Action onClose)
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

            await Task.Delay(expirationTime ?? TimeSpan.FromSeconds(5));

            notification.Close();
        }

        private void OnNotificationClosed(object sender, RoutedEventArgs routedEventArgs)
        {
            var notification = sender as Notification;
            _items.Remove(notification);
        }

        public void Install(TopLevel host)
        {
            var adornerLayer = host.GetVisualDescendants()
                .OfType<AdornerDecorator>()
                .FirstOrDefault()
                ?.AdornerLayer;

            if (adornerLayer != null)
            {
                adornerLayer.Children.Add(this as IControl);
            }
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
