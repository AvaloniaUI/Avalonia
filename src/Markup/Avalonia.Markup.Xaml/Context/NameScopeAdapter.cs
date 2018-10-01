using System;
using Avalonia.Controls;

namespace Avalonia.Markup.Xaml.Context
{
    internal class NameScopeAdapter : System.Windows.Markup.INameScope
    {
        public NameScopeAdapter() : this(null) { }
        public NameScopeAdapter(INameScope inner) => Inner = inner ?? new NameScope();

        public INameScope Inner { get; private set; }

        public object FindName(string name) => Inner.Find(name);
        public void RegisterName(string name, object scopedElement) => Inner.Register(name, scopedElement);
        public void UnregisterName(string name) => Inner.Unregister(name);

        public INameScope Extract()
        {
            var result = Inner;
            Inner = new NameScope();
            return result;
        }

        public void Apply(object target)
        {
            if (ReferenceEquals(target, Inner))
            {
                return;
            }

            var sourceNs = (NameScope)Inner;
            var targetNs = target as INameScope;

            if (targetNs == null && target is StyledElement s)
            {
                targetNs = NameScope.GetNameScope(s);
            }

            if (targetNs != null)
            {
                foreach (var i in sourceNs)
                {
                    targetNs.Register(i.Key, i.Value);
                }
            }
        }
    }
}
