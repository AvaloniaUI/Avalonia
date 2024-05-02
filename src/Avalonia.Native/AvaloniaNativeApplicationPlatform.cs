using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Native.Interop;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;

namespace Avalonia.Native
{
    internal class AvaloniaNativeApplicationPlatform : NativeCallbackBase, IAvnApplicationEvents, IPlatformLifetimeEventsImpl
    {
        public event EventHandler<ShutdownRequestedEventArgs> ShutdownRequested;

        void IAvnApplicationEvents.FilesOpened(IAvnStringArray urls)
        {
            ((IApplicationPlatformEvents)Application.Current)?.RaiseUrlsOpened(urls.ToStringArray());

            if (AvaloniaLocator.Current.GetService<IActivatableLifetime>() is ActivatableLifetimeBase lifetime)
            {
                var filePaths = urls.ToStringArray();
                var files = new List<IStorageItem>(filePaths.Length);
                foreach (var filePath in filePaths)
                {
                    if (StorageProviderHelpers.TryCreateBclStorageItem(filePath) is { } file)
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
            // Raise the urls opened event to be compatible with legacy behavior.
            ((IApplicationPlatformEvents)Application.Current)?.RaiseUrlsOpened(urls.ToStringArray());

            if (AvaloniaLocator.Current.GetService<IActivatableLifetime>() is ActivatableLifetimeBase lifetime)
            {
                var files = new List<IStorageItem>();
                var uris = new List<Uri>();
                foreach (var url in urls.ToStringArray())
                {
                    if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
                    {
                        if (uri.Scheme == Uri.UriSchemeFile)
                        {
                            if (StorageProviderHelpers.TryCreateBclStorageItem(uri.LocalPath) is { } file)
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
            if (AvaloniaLocator.Current.GetService<IActivatableLifetime>() is ActivatableLifetimeBase lifetime)
            {
                lifetime.OnActivated(ActivationKind.Background);    
            }
        }

        void IAvnApplicationEvents.OnUnhide()
        {
            if (AvaloniaLocator.Current.GetService<IActivatableLifetime>() is ActivatableLifetimeBase lifetime)
            {
                lifetime.OnActivated(ActivationKind.Background);    
            }
        }

        public int TryShutdown()
        {
            if (ShutdownRequested is null) return 1;
            var e = new ShutdownRequestedEventArgs();
            ShutdownRequested(this, e);
            return (!e.Cancel).AsComBool();
        }
    }
}
