using System;
using System.ComponentModel;

namespace Avalonia.Controls.ApplicationLifetimes
{
    public class ShutdownRequestedEventArgs : CancelEventArgs
    {
        private readonly object _deferralLock = new();
        private Action<bool>? _deferralsCompleted;
        private Action<bool>? _completeDeferredShutdown;
        private int _deferralCount;
        private readonly bool _canDefer;
        private bool _deferralRequested;
        private bool _deferralCancelled;
        private bool _deferralRequestClosed;
        private bool _deferralsAborted;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShutdownRequestedEventArgs"/> class.
        /// </summary>
        public ShutdownRequestedEventArgs()
        {
        }

        internal ShutdownRequestedEventArgs(Action<bool>? completeDeferredShutdown)
        {
            _canDefer = completeDeferredShutdown is not null;
            _completeDeferredShutdown = completeDeferredShutdown;
            _deferralCount = _canDefer ? 1 : 0;
        }

        /// <summary>
        /// Gets a deferral for asynchronous work before the application shuts down.
        /// </summary>
        /// <remarks>
        /// Deferrals are supported only for native macOS shutdown requests. Call this method while handling
        /// <see cref="IClassicDesktopStyleApplicationLifetime.ShutdownRequested"/> and complete the returned
        /// deferral when the work is finished. If any deferral cancels shutdown, the application remains open.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Deferrals are not supported for this shutdown request, or the request has already been handled.
        /// </exception>
        public ShutdownRequestedDeferral GetDeferral()
        {
            lock (_deferralLock)
            {
                if (!_canDefer || _deferralRequestClosed || _deferralsAborted)
                    throw new InvalidOperationException("Deferrals are not supported for this shutdown request.");

                _deferralRequested = true;
                _deferralCount++;
            }

            return new ShutdownRequestedDeferral(CompleteDeferral);
        }

        /// <summary>
        /// Is the operating system shutting down
        /// </summary>
        internal bool IsOSShutdown { get; init; }

        /// <summary>
        /// Indicates that the accepted shutdown will exit the main loop.
        /// </summary>
        internal bool WillExitMainLoop { get; set; }

        internal bool HasDeferrals
        {
            get
            {
                lock (_deferralLock)
                    return _deferralRequested;
            }
        }

        internal void SetDeferralsCompletedCallback(Action<bool> callback)
        {
            Action<bool>? callbackToInvoke;
            bool cancelShutdown;

            lock (_deferralLock)
            {
                if (_deferralsCompleted is not null)
                    throw new InvalidOperationException("The shutdown deferral callback is already set.");

                if (_deferralsAborted)
                    return;

                _deferralsCompleted = callback;
                callbackToInvoke = TakeDeferralsCompletedCallback(out cancelShutdown);
            }

            callbackToInvoke?.Invoke(cancelShutdown);
        }

        internal void EndDeferralRequest()
        {
            Action<bool>? callbackToInvoke;
            bool cancelShutdown;

            lock (_deferralLock)
            {
                if (!_canDefer || _deferralRequestClosed)
                    return;

                _deferralRequestClosed = true;
                _deferralCount--;
                callbackToInvoke = TakeDeferralsCompletedCallback(out cancelShutdown);
            }

            callbackToInvoke?.Invoke(cancelShutdown);
        }

        internal void CancelDeferrals()
        {
            lock (_deferralLock)
            {
                _deferralsAborted = true;
                _deferralsCompleted = null;
                _completeDeferredShutdown = null;
            }
        }

        internal void CompleteDeferredShutdown(bool shouldTerminate)
        {
            Action<bool>? completeDeferredShutdown;
            lock (_deferralLock)
            {
                completeDeferredShutdown = _completeDeferredShutdown;
                _completeDeferredShutdown = null;
            }

            completeDeferredShutdown?.Invoke(shouldTerminate);
        }

        private void CompleteDeferral(bool cancelShutdown)
        {
            Action<bool>? callbackToInvoke;
            bool shouldCancel;

            lock (_deferralLock)
            {
                if (_deferralsAborted)
                    return;

                _deferralCancelled |= cancelShutdown;
                _deferralCount--;
                callbackToInvoke = TakeDeferralsCompletedCallback(out shouldCancel);
            }

            callbackToInvoke?.Invoke(shouldCancel);
        }

        private Action<bool>? TakeDeferralsCompletedCallback(out bool cancelShutdown)
        {
            cancelShutdown = _deferralCancelled;

            if (!_deferralRequestClosed || _deferralCount != 0 || !_deferralRequested)
                return null;

            var callback = _deferralsCompleted;
            _deferralsCompleted = null;
            return callback;
        }
    }
}
