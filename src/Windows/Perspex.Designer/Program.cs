using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using Perspex.Designer.AppHost;
using Perspex.Designer.Comm;

namespace Perspex.Designer
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (!Console.IsInputRedirected || !Console.IsOutputRedirected)
                DemoMain(args);
            else
                HostedMain();


        }

        static void HostedMain()
        {
            //Initialize sync context
            SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
            var comm = new CommChannel(Console.OpenStandardInput(), Console.OpenStandardOutput());
            comm.Disposed += () => Process.GetCurrentProcess().Kill();
            var service = new PerspexAppHost(comm);
            service.Start();
            Application.Run();
        }

        private static void DemoMain(string[] args)
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
            var app =  new App();
            const string targetExe = "--exe=";
            const string xaml = "--xaml=";
            
            app.Run(new DemoWindow(
                args.Where(a => a.StartsWith(targetExe)).Select(a => a.Substring(targetExe.Length)).FirstOrDefault(),
                args.Where(a => a.StartsWith(xaml)).Select(a => a.Substring(xaml.Length)).FirstOrDefault()));
        }
    }
}
