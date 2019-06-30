// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Rendering;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Avalonia;
using ReactiveUI;
using DynamicData;
using Xunit;
using Splat;
using Avalonia.Markup.Xaml;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Reactive;
using Avalonia.ReactiveUI;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.Threading;

namespace Avalonia.ReactiveUI.UnitTests
{
    public class AutoSuspendHelperTest
    {
        [Fact]
        public void AutoSuspendHelper_Should_Immediately_Fire_IsLaunchingNew() 
        {
            using (UnitTestApplication.Start(TestServices.MockWindowingPlatform)) 
            {
                var isLaunchingReceived = false;
                var application = AvaloniaLocator.Current.GetService<Application>();
                var suspension = new AutoSuspendHelper(application.ApplicationLifetime);

                RxApp.SuspensionHost.IsLaunchingNew.Subscribe(_ => isLaunchingReceived = true);
                suspension.OnFrameworkInitializationCompleted();
                Assert.True(isLaunchingReceived);
            }
        }

        [Fact]
        public void ShouldPersistState_Should_Fire_On_App_Exit_When_SuspensionDriver_Is_Initialized() 
        {
            using (UnitTestApplication.Start(TestServices.MockWindowingPlatform)) 
            {
                var shouldPersistReceived = false;
                var application = AvaloniaLocator.Current.GetService<Application>();
                var suspension = new AutoSuspendHelper(application.ApplicationLifetime);

                RxApp.SuspensionHost.SetupDefaultSuspendResume(new DummySuspensionDriver());
                RxApp.SuspensionHost.ShouldPersistState.Subscribe(_ => shouldPersistReceived = true);
                suspension.OnFrameworkInitializationCompleted();

                var source = new CancellationTokenSource();
                source.CancelAfter(TimeSpan.FromMilliseconds(100));
                application.Run(source.Token);
                
                Assert.True(shouldPersistReceived);
            }
        }
    }
}