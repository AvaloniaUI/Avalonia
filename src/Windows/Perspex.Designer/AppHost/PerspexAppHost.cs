using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Perspex.Designer.Comm;

namespace Perspex.Designer.AppHost
{
    class PerspexAppHost
    {
        private string _appDir;
        private CommChannel _comm;
        private Func<Stream, object, object> _xamlReader;
        private WindowHost _host;

        public PerspexAppHost(CommChannel channel)
        {
            _comm = channel;
        }

        public void Start()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            _comm.OnMessage += Channel_OnMessage;
            _comm.Start();
        }

        private void Channel_OnMessage(object obj)
        {
            var init = obj as InitMessage;
            if (init != null)
            {
                Init(init.TargetExe);
            }
            var updateXaml = obj as UpdateXamlMessage;
            if (updateXaml != null)
                UpdateXaml(updateXaml.Xaml);
        }

        private void UpdateXaml(string xaml)
        {
            dynamic window = Activator.CreateInstance(LookupType("Perspex.Controls.Window"));
            _xamlReader(new MemoryStream(Encoding.UTF8.GetBytes(xaml)), window);
            var hWnd = (IntPtr) window.PlatformImpl.Handle;
            _host.SetWindow(hWnd);
            window.Show();
        }

        void UpdateState(string state)
        {
            _comm.SendMessage(new StateMessage(state));
        }

        Type LookupType(string name)
        {
            var asms = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in asms)
            {
                var found = asm.GetType(name, false, true);
                if (found != null)
                    return found;
            }
            throw new TypeLoadException("Unable to find type " + name);
        }

        private MethodInfo LookupStaticMethod(string typeName, string method)
        {
            var type = LookupType(typeName);
            var methods = type.GetMethods();
            return methods.First(m => m.Name == method);
        }

        private void Init(string targetExe)
        {
            _appDir = Path.GetFullPath(Path.GetDirectoryName(targetExe));
            Directory.SetCurrentDirectory(_appDir);
            UpdateState("Loading assemblies");
            foreach(var asm in Directory.GetFiles(_appDir).Where(f=>f.ToLower().EndsWith(".dll")||f.ToLower().EndsWith(".exe")))
                try
                {
                    Assembly.LoadFrom(asm);
                }
                catch { }
            UpdateState("Looking up Perspex types");

            var app = Activator.CreateInstance(LookupType("Perspex.Application"));
            app.GetType()
                .GetMethod("RegisterServices", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(app, null);
            app.GetType().GetProperty("Styles").GetSetMethod(true)
                .Invoke(app, new[] {Activator.CreateInstance(LookupType("Perspex.Themes.Default.DefaultTheme"))});


            LookupStaticMethod("Perspex.Direct2D1.Direct2D1Platform", "Initialize").Invoke(null, null);
            LookupStaticMethod("Perspex.Win32.Win32Platform", "InitializeEmbedded").Invoke(null, null);

            
            var xamlFactory = Activator.CreateInstance(LookupType("Perspex.Markup.Xaml.Context.PerspexParserFactory"));
            
            dynamic xamlLoader =
                LookupType("OmniXaml.XamlLoader").GetConstructors().First().Invoke(new object[] {xamlFactory});

            _xamlReader = (stream, root) => xamlLoader.Load(stream, root);
            _host = new WindowHost();
            _comm.SendMessage(new WindowCreatedMessage(_host.Handle));
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assemblyPath = Path.Combine(_appDir, new AssemblyName(args.Name).Name + ".dll");
            if (File.Exists(assemblyPath) == false) return null;
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }
    }
}
