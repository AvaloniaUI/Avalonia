using System;
using System.Collections.Generic;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Platform;
using Avalonia.Native.Interop;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;

namespace Avalonia.Native
{
    internal class AvaloniaNativeApplicationPlatform(AvaloniaNativePlatform platform)
        : NativeCallbackBase, IAvnApplicationEvents, IPlatformLifetimeEventsImpl
    {
        public event EventHandler<ShutdownRequestedEventArgs>? ShutdownRequested;

        void IAvnApplicationEvents.FilesOpened(IAvnStringArray urls)
        {
            if (AvaloniaLocator.Current.GetService<IActivatableLifetime>() is ActivatableLifetimeBase lifetime
                && AvaloniaLocator.Current.GetService<IStorageProviderFactory>() is StorageProviderApi storageApi)
            {
                var filePaths = urls.ToStringArray();
                var files = new List<IStorageItem>(filePaths.Length);
                foreach (var filePath in filePaths)
                {
                    if (StorageProviderHelpers.TryGetUriFromFilePath(filePath, false) is { } fileUri
                        && storageApi.TryGetStorageItem(fileUri) is { } file)
                    {
                        files.Add(file);
                    }
                }

                if (files.Count > 0)
                {
                    lifetime.OnActivated(new FileActivatedEventArgs(files));
                }
            }
        }

        void IAvnApplicationEvents.UrlsOpened(IAvnStringArray urls)
        {
            if (AvaloniaLocator.Current.GetService<IActivatableLifetime>() is ActivatableLifetimeBase lifetime
                && AvaloniaLocator.Current.GetService<IStorageProviderFactory>() is StorageProviderApi storageApi)
            {
                var files = new List<IStorageItem>();
                var uris = new List<Uri>();
                foreach (var url in urls.ToStringArray())
                {
                    if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
                    {
                        if (uri.Scheme == Uri.UriSchemeFile)
                        {
                            if (storageApi.TryGetStorageItem(uri) is { } file)
                            {
                                files.Add(file);
                            }
                        }
                        else
                        {
                            uris.Add(uri);
                        }
                    }
                }

                foreach (var uri in uris)
                {
                    lifetime.OnActivated(new ProtocolActivatedEventArgs(uri));
                }
                if (files.Count > 0)
                {
                    lifetime.OnActivated(new FileActivatedEventArgs(files));
                }
            }
        }

        void IAvnApplicationEvents.OnReopen()
        {
            if (AvaloniaLocator.Current.GetService<IActivatableLifetime>() is ActivatableLifetimeBase lifetime)
            {
                lifetime.OnActivated(ActivationKind.Reopen);    
            }
        }

        void IAvnApplicationEvents.OnHide()
        {
        }

        void IAvnApplicationEvents.OnUnhide()
        {
        }

        void IAvnApplicationEvents.OnActivate()
        {
            if (AvaloniaLocator.Current.GetService<IActivatableLifetime>() is ActivatableLifetimeBase lifetime)
            {
                lifetime.OnActivated(ActivationKind.Background);
            }
        }

        void IAvnApplicationEvents.OnDeactivate()
        {
            if (AvaloniaLocator.Current.GetService<IActivatableLifetime>() is ActivatableLifetimeBase lifetime)
            {
                lifetime.OnDeactivated(ActivationKind.Background);
            }
        }

        void IAvnApplicationEvents.OnTerminating()
        {
            // The OS is terminating us directly: AppDomain.ProcessExit won't run, dispose now.
            platform.Dispose();
        }

        public AvnShutdownReply TryShutdown(int isOSShutdown)
        {
            if (ShutdownRequested is not { } shutdownRequested)
                return AvnShutdownReply.ShutdownReplyTerminateNow;

            var isOSShutdownBool = isOSShutdown.FromComBool();
            var e = new ShutdownRequestedEventArgs { IsOSShutdown = isOSShutdownBool };
            shutdownRequested.Invoke(this, e);

            if (e.Cancel)
                return AvnShutdownReply.ShutdownReplyCancel;

            // If we know the main loop is going to exit (e.g. via a ClassicDesktopApplicationLifetime),
            // tell the native side it doesn't have to exit, allowing the managed side to complete its shutdown.
            if (e.WillExitMainLoop && !isOSShutdownBool)
                return AvnShutdownReply.ShutdownReplyDeferToManagedLoop;

            return AvnShutdownReply.ShutdownReplyTerminateNow;

        }
    }
}
