using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Avalonia.DesignerSupport;
using Avalonia.Markup.Xaml;

namespace Avalonia.Designer.HostApp
{
    [RequiresUnreferencedCode(XamlX.TrimmingMessages.DynamicXamlReference)]
    class Program
    {
#if NETFRAMEWORK
        private static string s_appDir;
        
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assemblyPath = Path.Combine(s_appDir, new AssemblyName(args.Name).Name + ".dll");
            if (File.Exists(assemblyPath) == false) return null;
            return Assembly.LoadFile(assemblyPath);
        }

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
#else
        public static void Main(string[] args)
#endif
        {
            AvaloniaLocator.CurrentMutable.Bind<AvaloniaXamlLoader.IRuntimeXamlLoader>()
                .ToConstant(new DesignXamlLoader());
            Avalonia.DesignerSupport.Remote.RemoteDesignerEntryPoint.Main(args);
        }
    }
}
