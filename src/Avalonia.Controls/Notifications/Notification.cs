using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Avalonia.Controls.Notifications
{
    /// <summary>
    /// A notification that can be shown in a window or by the host operating system.
    /// </summary>
    /// <remarks>
    /// This class represents a notification that can be displayed either in a window using
    /// <see cref="WindowNotificationManager"/> or by the host operating system (to be implemented).
    /// </remarks>
    public class Notification : INotification, INotifyPropertyChanged
    {
        private string? _title, _message;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Notification"/> class.
        /// </summary>
        /// <param name="title">The title of the notification.</param>
        /// <param name="message">The message to be displayed in the notification.</param>
        /// <param name="type">The <see cref="NotificationType"/> of the notification.</param>
        /// <param name="expiration">The expiry time at which the notification will close. 
        /// Use <see cref="TimeSpan.Zero"/> for notifications that will remain open.</param>
        /// <param name="onClick">An Action to call when the notification is clicked.</param>
        /// <param name="onClose">An Action to call when the notification is closed.</param>
        public Notification(string? title,
            string? message,
            NotificationType type = NotificationType.Information,
            TimeSpan? expiration = null,
            Action? onClick = null,
            Action? onClose = null)
        {
            Title = title;
            Message = message;
            Type = type;
            Expiration = expiration.HasValue ? expiration.Value : TimeSpan.FromSeconds(5);
            OnClick = onClick;
            OnClose = onClose;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Notification"/> class.
        /// </summary>
        public Notification() : this(null, null)
        {
        }

        /// <inheritdoc/>
        public string? Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <inheritdoc/>
        public string? Message
        {
            get => _message;
            set
            {
                if (_message != value)
                {
                    _message = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <inheritdoc/>
        public NotificationType Type { get; set; }

        /// <inheritdoc/>
        public TimeSpan Expiration { get; set; }

        /// <inheritdoc/>
        public Action? OnClick { get; set; }

        /// <inheritdoc/>
        public Action? OnClose { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
