using System;
using System.Collections;
using System.Linq;
using Avalonia.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Rendering;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Notifications
{
    /// <summary>
    /// An <see cref="INotificationManager"/> that displays notifications in a <see cref="Window"/>.
    /// </summary>
    [TemplatePart("PART_Items", typeof(Panel))]
    [PseudoClasses(":topleft", ":topright", ":bottomleft", ":bottomright")]
    public class WindowNotificationManager : TemplatedControl, IManagedNotificationManager
    {
        private IList? _items;

        /// <summary>
        /// Defines the <see cref="Position"/> property.
        /// </summary>
        public static readonly StyledProperty<NotificationPosition> PositionProperty =
          AvaloniaProperty.Register<WindowNotificationManager, NotificationPosition>(nameof(Position), NotificationPosition.TopRight);

        /// <summary>
        /// Defines which corner of the screen notifications can be displayed in.
        /// </summary>
        /// <seealso cref="NotificationPosition"/>
        public NotificationPosition Position
        {
            get => GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
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
            get => GetValue(MaxItemsProperty);
            set => SetValue(MaxItemsProperty, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowNotificationManager"/> class.
        /// </summary>
        /// <param name="host">The window that will host the control.</param>
        public WindowNotificationManager(TopLevel? host)
        {
            if (host != null)
            {
                Install(host);
            }

            UpdatePseudoClasses(Position);
        }

        static WindowNotificationManager()
        {
            HorizontalAlignmentProperty.OverrideDefaultValue<WindowNotificationManager>(Layout.HorizontalAlignment.Stretch);
            VerticalAlignmentProperty.OverrideDefaultValue<WindowNotificationManager>(Layout.VerticalAlignment.Stretch);
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            var itemsControl = e.NameScope.Find<Panel>("PART_Items");
            _items = itemsControl?.Children;
        }

        /// <inheritdoc/>
        public void Show(INotification content)
        {
            Show(content as object);
        }

        /// <inheritdoc/>
        public async void Show(object content)
        {
            var notification = content as INotification;

            var notificationControl = new NotificationCard
            {
                Content = content
            };

            notificationControl.NotificationClosed += (sender, args) =>
            {
                notification?.OnClose?.Invoke();

                _items?.Remove(sender);
            };

            notificationControl.PointerPressed += (sender, args) =>
            {
                if (notification != null && notification.OnClick != null)
                {
                    notification.OnClick.Invoke();
                }

                (sender as NotificationCard)?.Close();
            };

            _items?.Add(notificationControl);

            if (_items?.OfType<NotificationCard>().Count(i => !i.IsClosing) > MaxItems)
            {
                _items.OfType<NotificationCard>().First(i => !i.IsClosing).Close();
            }

            if (notification != null && notification.Expiration == TimeSpan.Zero)
            {
                return;
            }

            await Task.Delay(notification?.Expiration ?? TimeSpan.FromSeconds(5));

            notificationControl.Close();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == PositionProperty)
            {
                UpdatePseudoClasses(change.GetNewValue<NotificationPosition>());
            }
        }

        /// <summary>
        /// Installs the <see cref="WindowNotificationManager"/> within the <see cref="AdornerLayer"/>
        /// of the host <see cref="Window"/>.
        /// </summary>
        /// <param name="host">The <see cref="Window"/> that will be the host.</param>
        private void Install(TemplatedControl host)
        {
            var adornerLayer = host.FindDescendantOfType<VisualLayerManager>()?.AdornerLayer;

            if (adornerLayer is not null)
            {
                adornerLayer.Children.Add(this);
                AdornerLayer.SetAdornedElement(this, adornerLayer);
            }
        }

        private void UpdatePseudoClasses(NotificationPosition position)
        {
            PseudoClasses.Set(":topleft", position == NotificationPosition.TopLeft);
            PseudoClasses.Set(":topright", position == NotificationPosition.TopRight);
            PseudoClasses.Set(":bottomleft", position == NotificationPosition.BottomLeft);
            PseudoClasses.Set(":bottomright", position == NotificationPosition.BottomRight);
        }
    }
}
