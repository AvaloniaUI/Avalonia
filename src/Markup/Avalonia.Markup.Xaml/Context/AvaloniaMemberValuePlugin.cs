// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using OmniXaml.TypeConversion;
using OmniXaml.Typing;

namespace Avalonia.Markup.Xaml.Context
{
#if OMNIXAML
    public class AvaloniaMemberValuePlugin : MemberValuePlugin
    {
        private readonly MutableMember _xamlMember;

        public AvaloniaMemberValuePlugin(MutableMember xamlMember) 
            : base(xamlMember)
        {
            _xamlMember = xamlMember;
        }

        public override void SetValue(object instance, object value, IValueContext valueContext)
        {
            PropertyAccessor.SetValue(instance, _xamlMember, value, valueContext);
        }
    }
#endif
}
