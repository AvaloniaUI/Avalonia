using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Avalonia.DesignerSupport
{
    class DesignerApiDictionary
    {
        public Dictionary<string, object> Dictionary { get; set; }

        public DesignerApiDictionary(Dictionary<string, object> dictionary)
        {
            Dictionary = dictionary;
        }

        protected object Get([CallerMemberName] string name = null)
        {
            Console.WriteLine("manuel pull test");
            object rv;
            Dictionary.TryGetValue(name, out rv);
            Console.WriteLine("Vi prøver igen");
            return rv;
        }

        protected void Set(object value, [CallerMemberName] string name = null)
        {
            Dictionary[name] = value;
        }
    }

    class DesignerApi : DesignerApiDictionary
    {
        public Action<string> UpdateXaml
        {
            get { return (Action<string>) Get(); }
            set {Set(value); }
        }

        public Action<Dictionary<string, object>> UpdateXaml2
        {
            get { return (Action<Dictionary<string, object>>)Get(); }
            set { Set(value); }
        }

        public Action OnResize
        {
            get { return (Action) Get(); }
            set { Set(value);}
        }

        public Action<IntPtr> OnWindowCreated
        {
            set { Set(value); }
            get { return (Action<IntPtr>) Get(); }
        }

        public Action<double> SetScalingFactor
        {
            set { Set(value);}
            get { return (Action<double>) Get(); }
        }

        public DesignerApi(Dictionary<string, object> dictionary) : base(dictionary)
        {
        }
    }

    class DesignerApiXamlFileInfo : DesignerApiDictionary
    {
        public string Xaml
        {
            get { return (string)Get(); }
            set { Set(value); }
        }

        public string AssemblyPath
        {
            get { return (string) Get(); }
            set { Set(value); }
        }

        public DesignerApiXamlFileInfo(Dictionary<string, object> dictionary) : base(dictionary)
        {
        }

        public DesignerApiXamlFileInfo(): base(new Dictionary<string, object>())
        {
            
        }
    }
}
