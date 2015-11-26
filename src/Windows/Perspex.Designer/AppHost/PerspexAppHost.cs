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
using Perspex.Designer.Comm;
using Perspex.Designer.InProcDesigner;
using Perspex.Designer.Metadata;
using Timer = System.Windows.Forms.Timer;

namespace Perspex.Designer.AppHost
{
    class PerspexAppHost
    {
        private string _appDir;
        private readonly CommChannel _comm;
        private string _lastXaml;
        private string _currentXaml;
        private bool _initSuccess;
        private HostedAppModel _appModel;
        private Control _window;

        public PerspexAppHost(CommChannel channel)
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
                _currentXaml = updateXaml.Xaml;
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
                UpdateState("Unable to load Perspex:\n\n" + e + "\n\n" + log);
            }
        }

        PerspexDesignerMetadata BuildMetadata(List<Assembly> asms, Type xmlNsAttr)
        {
            var rv = new PerspexDesignerMetadata()
            {
                
                NamespaceAliases = new List<MetadataNamespaceAlias>(),
                Types = new List<MetadataType>()
            };


            foreach (var asm in asms)
            {
                foreach (dynamic xmlns in asm.GetCustomAttributes().Where(a => a.GetType() == xmlNsAttr))
                {
                    rv.NamespaceAliases.Add(new MetadataNamespaceAlias
                    {
                        Namespace = (string)xmlns.ClrNamespace,
                        XmlNamespace = (string)xmlns.XmlNamespace
                    });
                }

                try
                {
                    foreach (var type in asm.GetTypes())
                    {
                        try
                        {
                            if (!type.IsPublic || type.IsAbstract)
                                continue;
                            var t = new MetadataType()
                            {
                                Name = type.Name,
                                Namespace = type.Namespace,
                                Properties = new List<MetadataProperty>()
                            };
                            rv.Types.Add(t);
                            foreach (var prop in type.GetProperties())
                            {
                                if (prop.GetMethod?.IsPublic != true)
                                    continue;
                                var p = new MetadataProperty()
                                {
                                    Name = prop.Name,
                                    Type =
                                        prop.PropertyType == typeof (string) ||
                                        (prop.PropertyType.IsValueType &&
                                         prop.PropertyType.Assembly == typeof (int).Assembly)
                                            ? MetadataPropertyType.BasicType
                                            : prop.PropertyType.IsEnum
                                                ? MetadataPropertyType.Enum
                                                : MetadataPropertyType.MetadataType

                                };
                                if (p.Type == MetadataPropertyType.Enum)
                                    p.EnumValues = Enum.GetNames(prop.PropertyType);
                                if (p.Type == MetadataPropertyType.MetadataType)
                                    p.MetadataFullTypeName = prop.PropertyType.Namespace + "." + prop.PropertyType.Name;
                                t.Properties.Add(p);
                            }
                        }
                        catch
                        {
                            //
                        }
                    }
                }
                catch
                {
                    //
                }
            }
            return rv;
        }

        void BuildMetadataAndSendMessageAsync(List<Assembly> asms)
        {
            var xmlNsAttr = LookupType("Perspex.Metadata.XmlnsDefinitionAttribute");
            new Thread(() =>
            {
                _comm.SendMessage(new UpdateMetadataMessage(BuildMetadata(asms, xmlNsAttr)));
            }).Start();
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
            
            log("Looking up Perspex types");
            BuildMetadataAndSendMessageAsync(asms);

            log("Initializing built-in designer");
            var dic = new Dictionary<string, object>();
            Api = new DesignerApi(dic) {OnResize = OnResize, OnWindowCreated = OnWindowCreated};
            LookupStaticMethod("Perspex.Designer.Designer", "Init").Invoke(null, new object[] {dic});

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
