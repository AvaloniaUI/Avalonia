using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Forms;
using Perspex.Designer.Comm;

namespace Perspex.Designer.AppHost
{
    class PerspexAppHost
    {
        private string _appDir;
        private CommChannel _comm;
        private string _lastXaml;
        private string _currentXaml;
        private Func<Stream, object> _xamlReader;
        private WindowHost _host;
        private bool _initSuccess;

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
                _currentXaml = updateXaml.Xaml;
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
            var log = new StringBuilder();
            try
            {
                DoInit(targetExe, log);
            }
            catch (Exception e)
            {
                UpdateState("Unable to load Perspex:\n\n" + e + "\n\n" + log);
            }
        }
        private void DoInit(string targetExe, StringBuilder logger)
        {
            _appDir = Path.GetFullPath(Path.GetDirectoryName(targetExe));
            Directory.SetCurrentDirectory(_appDir);
            Action<string> log = s =>
            {
                UpdateState(s);
                logger.AppendLine(s);
            };
            log("Loading assemblies from " + _appDir);
            foreach(var asm in Directory.GetFiles(_appDir).Where(f=>f.ToLower().EndsWith(".dll")||f.ToLower().EndsWith(".exe")))
                try
                {
                    log("Trying to load " + asm);
                    Assembly.LoadFrom(asm);
                }
                catch (Exception e)
                {
                    logger.AppendLine(e.ToString());
                }
            log("Looking up Perspex types");
            var syncContext = LookupType("Perspex.Threading.PerspexSynchronizationContext");
            syncContext.GetProperty("AutoInstall", BindingFlags.Public | BindingFlags.Static).SetValue(null, false);

            var app = Activator.CreateInstance(LookupType("Perspex.Application"));
            app.GetType()
                .GetMethod("RegisterServices", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(app, null);
            app.GetType().GetProperty("Styles").GetSetMethod(true)
                .Invoke(app, new[] {Activator.CreateInstance(LookupType("Perspex.Themes.Default.DefaultTheme"))});


            LookupStaticMethod("Perspex.Direct2D1.Direct2D1Platform", "Initialize").Invoke(null, null);
            LookupStaticMethod("Perspex.Win32.Win32Platform", "InitializeEmbedded").Invoke(null, null);

            dynamic dispatcher =
                LookupType("Perspex.Threading.Dispatcher")
                    .GetProperty("UIThread", BindingFlags.Static | BindingFlags.Public)
                    .GetValue(null);
            


            var xamlFactory = Activator.CreateInstance(LookupType("Perspex.Markup.Xaml.Context.PerspexParserFactory"));
            
            dynamic xamlLoader =
                LookupType("OmniXaml.XamlLoader").GetConstructors().First().Invoke(new object[] {xamlFactory});

            _xamlReader = (stream) => xamlLoader.Load(stream);
            _host = new WindowHost();
            new Timer {Interval = 10, Enabled = true}.Tick += delegate
            {
                dispatcher.RunJobs();
            };
            new Timer {Interval = 200, Enabled = true}.Tick += delegate { XamlUpdater(); };
            _comm.SendMessage(new WindowCreatedMessage(_host.Handle));
            _initSuccess = true;
        }


        void XamlUpdater()
        {
            if (!_initSuccess)
                return;
            if(_lastXaml == _currentXaml)
                return;
            _lastXaml = _currentXaml;
            try
            {
                const string windowType = "Perspex.Controls.Window";
                
                var root = _xamlReader(new MemoryStream(Encoding.UTF8.GetBytes(_currentXaml)));
                dynamic window = root;
                if (root.GetType().FullName != windowType)
                {
                    window = Activator.CreateInstance(LookupType(windowType));
                    window.Content = root;
                }


                var hWnd = (IntPtr)window.PlatformImpl.Handle;
                _host.SetWindow(hWnd);
                window.Show();
            }
            catch (Exception e)
            {
                _host.SetWindow(IntPtr.Zero);
                _host.PlaceholderText = e.ToString();
            }
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
