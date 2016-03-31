// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Markup.Xaml.Context
{
    internal class NameScopeWrapper : Portable.Xaml.Markup.INameScope
    {
        private readonly Perspex.Controls.INameScope _inner;

        public NameScopeWrapper(Perspex.Controls.INameScope inner)
        {
            _inner = inner;
        }

        public object FindName(string name)
        {
            return _inner.Find(name);
        }

        public void RegisterName(string name, object scopedElement)
        {
            _inner.Register(name, scopedElement);
        }

        public void UnregisterName(string name)
        {
            _inner.Unregister(name);
        }
    }
}
