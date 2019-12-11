// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia;
using Avalonia.VisualTree;
using Avalonia.Controls;
using System.Threading;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Reactive;
using ReactiveUI;
using System;
using Avalonia.Controls.ApplicationLifetimes;
using Splat;

namespace Avalonia.ReactiveUI
{
    /// <summary>
    /// A ReactiveUI AutoSuspendHelper which initializes suspension hooks for
    /// Avalonia applications. Call its constructor in your app's composition root,
    /// before calling the RxApp.SuspensionHost.SetupDefaultSuspendResume method.
    /// </summary>
    public sealed class AutoSuspendHelper : IEnableLogger, IDisposable
    {
        private readonly Subject<IDisposable> _shouldPersistState = new Subject<IDisposable>();
        private readonly Subject<Unit> _isLaunchingNew = new Subject<Unit>();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AutoSuspendHelper"/> class.
        /// </summary>
        /// <param name="lifetime">Pass in the Application.ApplicationLifetime property.</param>
        public AutoSuspendHelper(IApplicationLifetime lifetime)
        {
            RxApp.SuspensionHost.IsResuming = Observable.Never<Unit>();
            RxApp.SuspensionHost.IsLaunchingNew = _isLaunchingNew;

            if (Avalonia.Controls.Design.IsDesignMode)
            {
                this.Log().Debug("Design mode detected. AutoSuspendHelper won't persist app state.");
                RxApp.SuspensionHost.ShouldPersistState = Observable.Never<IDisposable>();
            }
            else if (lifetime is IControlledApplicationLifetime controlled)
            {
                this.Log().Debug("Using IControlledApplicationLifetime events to handle app exit.");
                controlled.Exit += (sender, args) => OnControlledApplicationLifetimeExit();
                RxApp.SuspensionHost.ShouldPersistState = _shouldPersistState;
            }
            else if (lifetime != null)
            {
                var type = lifetime.GetType().FullName;
                var message = $"Don't know how to detect app exit event for {type}.";
                throw new NotSupportedException(message);
            }
            else
            {
                var message = "ApplicationLifetime is null. "
                            + "Ensure you are initializing AutoSuspendHelper "
                            + "after Avalonia application initialization is completed.";
                throw new ArgumentNullException(message);
            }
            
            var errored = new Subject<Unit>();
            AppDomain.CurrentDomain.UnhandledException += (o, e) => errored.OnNext(Unit.Default);
            RxApp.SuspensionHost.ShouldInvalidateState = errored;
        }

        /// <summary>
        /// Call this method in your App.OnFrameworkInitializationCompleted method.
        /// </summary>
        public void OnFrameworkInitializationCompleted() => _isLaunchingNew.OnNext(Unit.Default);

        /// <summary>
        /// Disposes internally stored observers.
        /// </summary>
        public void Dispose()
        {
            _shouldPersistState.Dispose();
            _isLaunchingNew.Dispose();
        }

        private void OnControlledApplicationLifetimeExit()
        {
            this.Log().Debug("Received IControlledApplicationLifetime exit event.");
            var manual = new ManualResetEvent(false);
            _shouldPersistState.OnNext(Disposable.Create(() => manual.Set()));
                    
            manual.WaitOne();
            this.Log().Debug("Completed actions on IControlledApplicationLifetime exit event.");
        }
    }
}
