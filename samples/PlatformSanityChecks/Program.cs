using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.X11;

namespace PlatformSanityChecks
{
    public class Program
    {
        static Thread UiThread;
        
        static void Main(string[] args)
        {
            UiThread = Thread.CurrentThread;
            AppBuilder.Configure<App>().RuntimePlatformServicesInitializer();
            var app = new App();
            
            AvaloniaX11PlatformExtensions.InitializeX11Platform();

            CheckPlatformThreading();
        }

        static bool CheckAccess() => UiThread == Thread.CurrentThread;

        static void VerifyAccess()
        {
            if (!CheckAccess())
                Die("Call from invalid thread");
        }
        
        static Exception Die(string error)
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine(Environment.StackTrace);
            Process.GetCurrentProcess().Kill();
            throw new Exception(error);
        }

        static IDisposable Enter([CallerMemberName] string caller = null)
        {
            Console.WriteLine("Entering " + caller);
            return Disposable.Create(() => { Console.WriteLine("Leaving " + caller); });
        }

        static void EnterLoop(Action<CancellationTokenSource> cb, [CallerMemberName] string caller = null)
        {
            using (Enter(caller))
            {
                var cts = new CancellationTokenSource();
                cb(cts);
                Dispatcher.UIThread.MainLoop(cts.Token);
                if (!cts.IsCancellationRequested)
                    Die("Unexpected loop exit");
            }
        }
        
        static void CheckTimerOrdering() => EnterLoop(cts =>
        {
            bool firstFired = false, secondFired = false;
            DispatcherTimer.Run(() =>
            {
                Console.WriteLine("Second tick");
                VerifyAccess();
                if (!firstFired)
                    throw Die("Invalid timer ordering");
                if (secondFired)
                    throw Die("Invocation of finished timer");
                secondFired = true;
                cts.Cancel();
                return false;
            }, TimeSpan.FromSeconds(2));
            DispatcherTimer.Run(() =>
            {
                Console.WriteLine("First tick");
                VerifyAccess();
                if (secondFired)
                    throw Die("Invalid timer ordering");
                if (firstFired)
                    throw Die("Invocation of finished timer");
                firstFired = true;
                return false;
            }, TimeSpan.FromSeconds(1));
        });

        static void CheckTimerTicking() => EnterLoop(cts =>
        {
            int ticks = 0;
            var st = Stopwatch.StartNew();
            DispatcherTimer.Run(() =>
            {
                ticks++;
                Console.WriteLine($"Tick {ticks} at {st.Elapsed}");
                if (ticks == 5)
                {
                    if (st.Elapsed.TotalSeconds < 4.5)
                        Die("Timer is too fast");
                    if (st.Elapsed.TotalSeconds > 6)
                        Die("Timer is too slow");
                    cts.Cancel();
                    return false;
                }

                return true;
            }, TimeSpan.FromSeconds(1));
        });

        static void CheckSignaling() => EnterLoop(cts =>
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(100);
                Dispatcher.UIThread.Post(() =>
                {
                    VerifyAccess();
                    cts.Cancel();
                });
            });
        });

        static void CheckPlatformThreading()
        {
            CheckSignaling();
            CheckTimerOrdering();
            CheckTimerTicking();
        }
    }
}
