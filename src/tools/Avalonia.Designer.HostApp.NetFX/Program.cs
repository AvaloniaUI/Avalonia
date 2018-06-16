using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Designer.HostApp.NetFX
{
    class Program
    {
        private static string s_appDir;
        public static void Main(string[] args)
        {
            s_appDir = Directory.GetCurrentDirectory();
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            foreach (var dll in Directory.GetFiles(s_appDir, "*.dll"))
            {
                try
                {
                    Console.WriteLine("Loading " + dll);
                    Assembly.LoadFile(dll);
                }
                catch
                {
                    
                }
            }
            Exec(args);
        }

        static void Exec(string[] args)
        {
            Avalonia.DesignerSupport.Remote.RemoteDesignerEntryPoint.Main(args);
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assemblyPath = Path.Combine(s_appDir, new AssemblyName(args.Name).Name + ".dll");
            if (File.Exists(assemblyPath) == false) return null;
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }
    }
}
