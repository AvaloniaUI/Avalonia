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

namespace Avalonia.ReactiveUI
{
    /// <summary>
    /// A ReactiveUI AutoSuspendHelper which initializes suspension hooks for
    /// Avalonia applications. Call its constructor in your app's composition root,
    /// before calling the RxApp.SuspensionHost.SetupDefaultSuspendResume method.
    /// </summary>
    public sealed class AutoSuspendHelper
    {
        public AutoSuspendHelper(Application app)
        {
            RxApp.SuspensionHost.IsResuming = Observable.Never<Unit>();
            RxApp.SuspensionHost.IsLaunchingNew = Observable.Return(Unit.Default);

            var exiting = new Subject<IDisposable>();
            app.Exit += (o, e) =>
            {
                // This is required to prevent the app from shutting down too early.
                var manual = new ManualResetEvent(false);
                exiting.OnNext(Disposable.Create(() => manual.Set()));
                manual.WaitOne();
            };
            RxApp.SuspensionHost.ShouldPersistState = exiting;
            
            var errored = new Subject<Unit>();
            AppDomain.CurrentDomain.UnhandledException += (o, e) => errored.OnNext(Unit.Default);
            RxApp.SuspensionHost.ShouldInvalidateState = errored;
        }
    }
}
