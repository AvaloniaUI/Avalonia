using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using Avalonia.Designer.AppHost;
using Avalonia.Designer.Comm;

namespace Avalonia.Designer
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (!args.Contains("--hosted"))
                DemoMain(args);
            else
                HostedMain();


        }

        static void HostedMain()
        {
            //Initialize sync context
            SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
            var comm = new CommChannel(Console.OpenStandardInput(), Console.OpenStandardOutput());
            Console.SetOut(new NullTextWriter());
            Console.SetError(new NullTextWriter());
            comm.Disposed += () => Process.GetCurrentProcess().Kill();
            comm.SendMessage(new StateMessage("Staying awhile and listening..."));
            var service = new AvaloniaAppHost(comm);
            service.Start();
            Application.Run();
        }

        class NullTextWriter : TextWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
            public override void Write(char[] buffer, int index, int count)
            {
            }

            public override void Write(char value)
            {
            }
        }

        private static void DemoMain(string[] args)
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
            var app =  new App();
            const string targetExe = "--exe=";
            const string xaml = "--xaml=";
            const string source = "--source=";

            app.Run(new DemoWindow(
                args.Where(a => a.StartsWith(targetExe)).Select(a => a.Substring(targetExe.Length)).FirstOrDefault(),
                args.Where(a => a.StartsWith(xaml)).Select(a => a.Substring(xaml.Length)).FirstOrDefault(),
                args.Where(a => a.StartsWith(source)).Select(a => a.Substring(source.Length)).FirstOrDefault()));
        }
    }
}
