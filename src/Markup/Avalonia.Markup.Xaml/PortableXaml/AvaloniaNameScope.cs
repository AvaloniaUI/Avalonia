using System.Collections.Generic;
using Avalonia.Controls;

namespace Avalonia.Markup.Xaml.PortableXaml
{
    internal class AvaloniaNameScope : Portable.Xaml.Markup.INameScope
    {
        public object Instance { get; set; }

        private Dictionary<string, object> _names = new Dictionary<string, object>();

        public object FindName(string name)
        {
            object result;
            if (_names.TryGetValue(name, out result))
                return result;
            return null;
        }

        public void RegisterName(string name, object scopedElement)
        {
            if (scopedElement != null)
                _names.Add(name, scopedElement);

            //TODO: ???
            //var control = scopedElement as Control;

            //if (control != null)
            //{
            //    var nameScope = (Instance as INameScope) ?? control.FindNameScope();

            //    if (nameScope != null)
            //    {
            //        nameScope.Register(name, scopedElement);
            //    }
            //}
        }

        public void UnregisterName(string name)
        {
        }

        public void RegisterOnNameScope(object target)
        {
            var nameScope = target as INameScope;

            if (nameScope != null)
            {
                foreach (var v in _names)
                {
                    nameScope.Register(v.Key, v.Value);
                }
            }
        }
    }
}