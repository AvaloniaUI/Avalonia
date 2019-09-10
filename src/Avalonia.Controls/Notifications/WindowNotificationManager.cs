// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Notifications;
using Avalonia.Notifications.Managed;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Notifications
{
    /// <summary>
    /// An <see cref="INotificationManager"/> that displays notifications in a <see cref="Window"/>.
    /// </summary>
    public class WindowNotificationManager : TemplatedControl, IManagedNotificationManager
    {
        private AvaloniaList<IControl> _items;
        private readonly object _notificationIdLock = new object();
        private uint _lastNotificationId;

        /// <summary>
        /// Defines the <see cref="Position"/> property.
        /// </summary>
        public static readonly StyledProperty<NotificationPosition> PositionProperty =
            AvaloniaProperty.Register<WindowNotificationManager, NotificationPosition>(nameof(Position),
                NotificationPosition.TopRight);

        /// <summary>
        /// Defines which corner of the screen notifications can be displayed in.
        /// </summary>
        /// <seealso cref="NotificationPosition"/>
        public NotificationPosition Position
        {
            get { return GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        /// <summary>
        /// Defines the <see cref="MaxItems"/> property.
        /// </summary>
        public static readonly StyledProperty<int> MaxItemsProperty =
            AvaloniaProperty.Register<WindowNotificationManager, int>(nameof(MaxItems), 5);

        /// <summary>
        /// Defines the maximum number of notifications visible at once.
        /// </summary>
        public int MaxItems
        {
            get { return GetValue(MaxItemsProperty); }
            set { SetValue(MaxItemsProperty, value); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowNotificationManager"/> class.
        /// </summary>
        /// <param name="host">The window that will host the control.</param>
        public WindowNotificationManager(Window host)
        {
            if (VisualChildren.Count != 0)
            {
                Install(host);
            }
            else
            {
                IDisposable unsub = null;
                unsub = Observable.FromEventPattern<TemplateAppliedEventArgs>(host, nameof(host.TemplateApplied))
                    .Take(1)
                    .Subscribe(_ =>
                    {
                        Install(host);
                        // ReSharper disable once AccessToModifiedClosure
                        unsub?.Dispose();
                    });
            }
        }

        static WindowNotificationManager()
        {
            PseudoClass<WindowNotificationManager, NotificationPosition>(
                PositionProperty,
                x => x == NotificationPosition.TopLeft, ":topleft"
            );
            PseudoClass<WindowNotificationManager, NotificationPosition>(
                PositionProperty,
                x => x == NotificationPosition.TopRight, ":topright"
            );
            PseudoClass<WindowNotificationManager, NotificationPosition>(
                PositionProperty,
                x => x == NotificationPosition.BottomLeft, ":bottomleft"
            );
            PseudoClass<WindowNotificationManager, NotificationPosition>(
                PositionProperty,
                x => x == NotificationPosition.BottomRight, ":bottomright"
            );

            HorizontalAlignmentProperty.OverrideDefaultValue<WindowNotificationManager>(
                HorizontalAlignment.Stretch
            );
            VerticalAlignmentProperty.OverrideDefaultValue<WindowNotificationManager>(
                VerticalAlignment.Stretch
            );
        }

        /// <inheritdoc/>
        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);

            var itemsControl = e.NameScope.Find<Panel>("PART_Items");
            _items = itemsControl?.Children;
        }

        /// <inheritdoc/>
        public void Show(INotification content)
        {
            Show(content as object);
        }

        /// <inheritdoc />
        public void Close(INotification notification)
        {
            if (notification.Id == default)
                return;

            var id = notification.Id;

            _items.OfType<NotificationCard>()
                .FirstOrDefault(n => n.Id == id)
                ?.Close();
        }

        /// <inheritdoc />
        public void Show(object content)
        {
            var notification = content as INotification;

            if (notification != null && notification.Id != default)
                return;

            var notificationControl = new NotificationCard { Content = content };

            notificationControl.NotificationClosed += (sender, __) =>
            {
                notification?.OnClose?.Invoke();

                _items.Remove(sender as NotificationCard);
            };

            notificationControl.PointerPressed += (sender, __) =>
            {
                notification?.OnClick?.Invoke();

                (sender as NotificationCard)?.Close();
            };

            _items.Add(notificationControl);

            if (_items.OfType<NotificationCard>().Count(i => !i.IsClosing) > MaxItems)
            {
                _items.OfType<NotificationCard>().First(i => !i.IsClosing).Close();
            }

            var _ = Task.Delay(notification?.Expiration ?? TimeSpan.FromSeconds(5))
                .ContinueWith(__ => Dispatcher.UIThread.InvokeAsync(notificationControl.Close))
                .ConfigureAwait(false);

            var id = GetNextNotificationId();
            notificationControl.SetId(id);
            notification?.SetId(id, this);
        }

        /// <summary>
        /// Generates a new id for a notification
        /// </summary>
        /// <returns>Next id</returns>
        private uint GetNextNotificationId()
        {
            lock (_notificationIdLock)
            {
                if (_lastNotificationId == uint.MaxValue)
                    return _lastNotificationId = 1;
                return ++_lastNotificationId;
            }
        }

        /// <summary>
        /// Installs the <see cref="WindowNotificationManager"/> within the <see cref="AdornerLayer"/>
        /// of the host <see cref="Window"/>.
        /// </summary>
        /// <param name="host">The <see cref="Window"/> that will be the host.</param>
        private void Install(Window host)
        {
            var adornerLayer = host.GetVisualDescendants()
                .OfType<VisualLayerManager>()
                .FirstOrDefault()
                ?.AdornerLayer;

            if (adornerLayer != null)
            {
                adornerLayer.Children.Add(this);
            }
        }
    }
}
