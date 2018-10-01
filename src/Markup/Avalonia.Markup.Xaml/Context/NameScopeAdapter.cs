using Avalonia.Controls;

#if SYSTEM_XAML
using IXamlNameScope = System.Windows.Markup.INameScope;
#else
using IXamlNameScope = Portable.Xaml.Markup.INameScope;
#endif

namespace Avalonia.Markup.Xaml.Context
{
    internal class NameScopeAdapter : IXamlNameScope
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
