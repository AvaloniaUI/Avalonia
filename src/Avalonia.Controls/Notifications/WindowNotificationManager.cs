using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Notifications
{
    public class WindowNotificationManager : TemplatedControl, IManagedNotificationManager
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
          AvaloniaProperty.RegisterAttached<WindowNotificationManager, TopLevel, NotificationPosition>("Position", defaultValue: NotificationPosition.TopLeft, inherits: true);

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
          AvaloniaProperty.RegisterAttached<WindowNotificationManager, TopLevel, int>("Position", defaultValue: 5, inherits: true);

        public int MaxItems
        {
            get { return GetValue(MaxItemsProperty); }
            set { SetValue(MaxItemsProperty, value); }
        }

        static WindowNotificationManager()
        {
            PseudoClass<WindowNotificationManager, NotificationPosition>(PositionProperty, x => x == NotificationPosition.TopLeft, ":topleft");
            PseudoClass<WindowNotificationManager, NotificationPosition>(PositionProperty, x => x == NotificationPosition.TopRight, ":topright");
            PseudoClass<WindowNotificationManager, NotificationPosition>(PositionProperty, x => x == NotificationPosition.BottomLeft, ":bottomleft");
            PseudoClass<WindowNotificationManager, NotificationPosition>(PositionProperty, x => x == NotificationPosition.BottomRight, ":bottomright");

            HorizontalAlignmentProperty.OverrideDefaultValue<WindowNotificationManager>(Layout.HorizontalAlignment.Stretch);
            VerticalAlignmentProperty.OverrideDefaultValue<WindowNotificationManager>(Layout.VerticalAlignment.Stretch);
        }

        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);

            var itemsControl = e.NameScope.Find<Panel>("PART_Items");
            _items = itemsControl?.Children;
        }

        public void Show(INotification content)
        {
            Show(content as object);
        }

        public async void Show(object content)
        {
            var notification = content as INotification;

            var notificationControl = new Notification
            {
                Content = content
            };

            if (notification != null)
            {
                notificationControl.NotificationClosed += (sender, args) => notification.OnClose?.Invoke();
            }

            notificationControl.NotificationClosed += OnNotificationClosed;

            notificationControl.PointerPressed += (sender, args) =>
            {
                if (notification != null && notification.OnClick != null)
                {
                    notification.OnClick.Invoke();
                    (sender as Notification)?.Close();
                }
            };

            lock (_items)
            {
                _items.Add(notificationControl);

                if (_items.OfType<Notification>().Count(i => !i.IsClosing) > MaxItems)
                {
                    _items.OfType<Notification>().First(i => !i.IsClosing).Close();
                }
            }

            if (notification != null && notification.Expiration == TimeSpan.MaxValue)
            {
                return;
            }

            await Task.Delay(notification?.Expiration ?? TimeSpan.FromSeconds(5));

            notificationControl.Close();
        }

        private void OnNotificationClosed(object sender, RoutedEventArgs routedEventArgs)
        {
            var notification = sender as Notification;
            _items.Remove(notification);
        }

        public void Install(Window host)
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
