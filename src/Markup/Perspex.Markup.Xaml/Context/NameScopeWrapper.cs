// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Markup.Xaml.Context
{
    internal class NameScopeWrapper : OmniXaml.INameScope
    {
        private Perspex.INameScope _inner;

        public NameScopeWrapper(Perspex.INameScope inner)
        {
            _inner = inner;
        }

        public object Find(string name)
        {
            return _inner.Find(name);
        }

        public void Register(string name, object scopedElement)
        {
            _inner.Register(name, scopedElement);
        }

        public void Unregister(string name)
        {
            _inner.Unregister(name);
        }
    }
}
