using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.DesignerSupport.Tests
{
    public class Checker : MarshalByRefObject
    {
        private string _appDir;
        private IntPtr _window;

        public void DoCheck(string baseAsset, string xamlText)
        {
            _appDir = new FileInfo(baseAsset).Directory.FullName;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            foreach (var asm in Directory.GetFiles(_appDir).Where(f => f.ToLower().EndsWith(".dll") || f.ToLower().EndsWith(".exe")))
                try
                {
                    Assembly.LoadFrom(asm);
                }
                catch (Exception)
                {
                }
            var dic = new Dictionary<string, object>();
            var api = new DesignerApi(dic) { OnResize = OnResize, OnWindowCreated = OnWindowCreated };
            LookupStaticMethod("Avalonia.DesignerSupport.DesignerAssist", "Init").Invoke(null, new object[] { dic });
            
            api.UpdateXaml2(new DesignerApiXamlFileInfo
            {
                Xaml = xamlText,
                AssemblyPath = baseAsset
            }.Dictionary);
            if (_window == IntPtr.Zero)
                throw new Exception("Something went wrong");

            SendMessage(_window, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        public const uint WM_CLOSE = 0x0010;

        private void OnWindowCreated(IntPtr lastIntPtr)
        {
            _window = lastIntPtr;
        }

        private void OnResize()
        {
        }


        static Type LookupType(params string[] names)
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

        static MethodInfo LookupStaticMethod(string typeName, string method)
        {
            var type = LookupType(typeName);
            var methods = type.GetMethods();
            return methods.First(m => m.Name == method);
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
