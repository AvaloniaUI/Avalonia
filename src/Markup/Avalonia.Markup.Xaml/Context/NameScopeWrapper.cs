// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Markup.Xaml.Context
{
#if OMNIXAML
    internal class NameScopeWrapper : OmniXaml.INameScope
    {
        private readonly Avalonia.Controls.INameScope _inner;

        public NameScopeWrapper(Avalonia.Controls.INameScope inner)
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
#endif
}
