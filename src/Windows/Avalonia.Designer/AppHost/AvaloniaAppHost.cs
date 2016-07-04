using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Xml;
using Avalonia.Designer.Comm;
using Avalonia.Designer.InProcDesigner;
using Timer = System.Windows.Forms.Timer;
using Avalonia.DesignerSupport;

namespace Avalonia.Designer.AppHost
{
    class AvaloniaAppHost
    {
        private string _appDir;
        private readonly CommChannel _comm;
        private string _lastXaml;
        private string _currentXaml;
        private string _currentSourceAssembly;
        private bool _initSuccess;
        private readonly HostedAppModel _appModel;
        private Control _window;

        public AvaloniaAppHost(CommChannel channel)
        {
            _comm = channel;
            _appModel = new HostedAppModel(this);
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
            {
                _currentXaml = updateXaml.Xaml;
                _currentSourceAssembly = updateXaml.AssemblyPath;
            }
        }

        void UpdateState(string state)
        {
            _comm.SendMessage(new StateMessage(state));
        }

        Type LookupType(params string[] names)
        {
            var asms = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in asms)
            {
                foreach (var name in names)
                {
                    var found = asm.GetType(name, false, true);
                    if (found != null)
                        return found;
                }
            }
            throw new TypeLoadException("Unable to find any of types: " + string.Join(",", names));
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
                UpdateState("Unable to load Avalonia:\n\n" + e + "\n\n" + log);
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
            var asms = new List<Assembly>();
            foreach(var asm in Directory.GetFiles(_appDir).Where(f=>f.ToLower().EndsWith(".dll")||f.ToLower().EndsWith(".exe")))
                try
                {
                    log("Trying to load " + asm);
                    asms.Add(Assembly.LoadFrom(asm));
                }
                catch (Exception e)
                {
                    logger.AppendLine(e.ToString());
                }
            
            log("Initializing built-in designer");
            var dic = new Dictionary<string, object>();
            Api = new DesignerApi(dic) {OnResize = OnResize, OnWindowCreated = OnWindowCreated};
            LookupStaticMethod("Avalonia.DesignerSupport.DesignerAssist", "Init").Invoke(null, new object[] {dic});

            _window = new Control
            {
                Controls =
                {
                    new ElementHost()
                    {
                        Child = new InProcDesignerView(_appModel),
                        Dock = DockStyle.Fill
                    }
                }
            };
            _window.CreateControl();
            
            new Timer {Interval = 200, Enabled = true}.Tick += delegate { XamlUpdater(); };
            _comm.SendMessage(new WindowCreatedMessage(_window.Handle));
            _initSuccess = true;
        }

        private void OnWindowCreated(IntPtr hWnd)
        {
            _appModel.NativeWindowHandle = hWnd;
        }


        public DesignerApi Api { get; set; }


        bool ValidateXml(string xml)
        {
            try
            {
                var rdr = new XmlTextReader(new StringReader(xml));
                while (rdr.Read())
                {
                    
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        private void OnResize()
        {
            
        }

        void XamlUpdater()
        {
            if (!_initSuccess)
                return;
            if(_lastXaml == _currentXaml)
                return;
            _lastXaml = _currentXaml;

            if (!ValidateXml(_currentXaml))
            {
                _appModel.SetError("Invalid markup");
                return;
            }
            try
            {
                if (Api.UpdateXaml2 != null)
                {
                    Api.UpdateXaml2(new DesignerApiXamlFileInfo
                    {
                        AssemblyPath = _currentSourceAssembly,
                        Xaml = _currentXaml
                    }.Dictionary);
                }
                else
                    Api.UpdateXaml(_currentXaml);

                _appModel.SetError(null);
            }
            catch (Exception e)
            {
                _appModel.SetError("XAML load error", e.ToString());
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

    static class Helper
    {
        public static object Prop(this object obj, string name) => obj.GetType().GetProperty(name).GetValue(obj);

    }
}
