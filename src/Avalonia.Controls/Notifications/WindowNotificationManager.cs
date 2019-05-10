using System;
using System.Collections;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Notifications
{
    public class WindowNotificationManager : TemplatedControl, IManagedNotificationManager
    {
        private IList _items;

        public static readonly StyledProperty<NotificationPosition> PositionProperty =
          AvaloniaProperty.Register<WindowNotificationManager, NotificationPosition>(nameof(Position), NotificationPosition.TopRight);

        public NotificationPosition Position
        {
            get { return GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        public static readonly StyledProperty<int> MaxItemsProperty =
          AvaloniaProperty.Register<WindowNotificationManager, int>(nameof(MaxItems), 5);

        public int MaxItems
        {
            get { return GetValue(MaxItemsProperty); }
            set { SetValue(MaxItemsProperty, value); }
        }

        public WindowNotificationManager(Window host)
        {
            if (VisualChildren.Count != 0)
            {
                Install(host);
            }
            else
            {
                Observable.FromEventPattern<TemplateAppliedEventArgs>(host, nameof(host.TemplateApplied)).Take(1)
                    .Subscribe(_ =>
                    {
                        Install(host);
                    });
            }
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

            var notificationControl = new NotificationCard
            {
                Content = content
            };

            if (notification != null)
            {
                notificationControl.NotificationClosed += (sender, args) =>
                {
                    notification.OnClose?.Invoke();

                    _items.Remove(sender);
                };
            }

            notificationControl.PointerPressed += (sender, args) =>
            {
                if (notification != null && notification.OnClick != null)
                {
                    notification.OnClick.Invoke();
                }

                (sender as NotificationCard)?.Close();
            };

            _items.Add(notificationControl);

            if (_items.OfType<NotificationCard>().Count(i => !i.IsClosing) > MaxItems)
            {
                _items.OfType<NotificationCard>().First(i => !i.IsClosing).Close();
            }

            if (notification != null && notification.Expiration == TimeSpan.MaxValue)
            {
                return;
            }

            await Task.Delay(notification?.Expiration ?? TimeSpan.FromSeconds(5));

            notificationControl.Close();
        }

        private void Install(Window host)
        {
            var adornerLayer = host.GetVisualDescendants()
                .OfType<AdornerDecorator>()
                .FirstOrDefault()
                ?.AdornerLayer;

            if (adornerLayer != null)
            {
                adornerLayer.Children.Add(this);
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
