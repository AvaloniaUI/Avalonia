// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.Rendering;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia;
using ReactiveUI;
using DynamicData;
using Xunit;
using Splat;

namespace Avalonia.ReactiveUI.UnitTests
{
    public class AutoSuspendHelperTest
    {
        [DataContract]
        public class AppState
        {
            [DataMember]
            public string Example { get; set; }
        }

        public class ExoticApplicationLifetimeWithoutLifecycleEvents : IDisposable, IApplicationLifetime
        {
            public void Dispose() { }
        }

        [Fact]
        public void AutoSuspendHelper_Should_Immediately_Fire_IsLaunchingNew() 
        {
            using (UnitTestApplication.Start(TestServices.MockWindowingPlatform)) 
            using (var lifetime = new ClassicDesktopStyleApplicationLifetime(Application.Current))
            {
                var isLaunchingReceived = false;
                var application = AvaloniaLocator.Current.GetService<Application>();
                application.ApplicationLifetime = lifetime;

                // Initialize ReactiveUI Suspension as in real-world scenario.
                var suspension = new AutoSuspendHelper(application.ApplicationLifetime);
                RxApp.SuspensionHost.IsLaunchingNew.Subscribe(_ => isLaunchingReceived = true);
                suspension.OnFrameworkInitializationCompleted();

                Assert.True(isLaunchingReceived);
            }
        }

        [Fact]
        public void AutoSuspendHelper_Should_Throw_When_Not_Supported_Lifetime_Is_Used()
        {
            using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
            using (var lifetime = new ExoticApplicationLifetimeWithoutLifecycleEvents()) 
            {
                var application = AvaloniaLocator.Current.GetService<Application>();
                application.ApplicationLifetime = lifetime;
                Assert.Throws<NotSupportedException>(() => new AutoSuspendHelper(application.ApplicationLifetime));
            }
        }

        [Fact]
        public void AutoSuspendHelper_Should_Throw_When_Lifetime_Is_Null()
        {
            using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
            {
                var application = AvaloniaLocator.Current.GetService<Application>();
                Assert.Throws<ArgumentNullException>(() => new AutoSuspendHelper(application.ApplicationLifetime));
            }
        }

        [Fact]
        public void ShouldPersistState_Should_Fire_On_App_Exit_When_SuspensionDriver_Is_Initialized() 
        {
            using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
            using (var lifetime = new ClassicDesktopStyleApplicationLifetime(Application.Current)) 
            {
                var shouldPersistReceived = false;
                var application = AvaloniaLocator.Current.GetService<Application>();
                application.ApplicationLifetime = lifetime;

                // Initialize ReactiveUI Suspension as in real-world scenario.
                var suspension = new AutoSuspendHelper(application.ApplicationLifetime);
                RxApp.SuspensionHost.CreateNewAppState = () => new AppState { Example = "Foo" };
                RxApp.SuspensionHost.ShouldPersistState.Subscribe(_ => shouldPersistReceived = true);
                RxApp.SuspensionHost.SetupDefaultSuspendResume(new DummySuspensionDriver());
                suspension.OnFrameworkInitializationCompleted();

                lifetime.Shutdown();
                Assert.True(shouldPersistReceived);
                Assert.Equal("Foo", RxApp.SuspensionHost.GetAppState<AppState>().Example);
            }
        }
    }
}