// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using OmniXaml.TypeConversion;
using OmniXaml.Typing;

namespace Perspex.Markup.Xaml.Context
{
    public class PerspexMemberValuePlugin : MemberValuePlugin
    {
        private readonly MutableMember _xamlMember;

        public PerspexMemberValuePlugin(MutableMember xamlMember) 
            : base(xamlMember)
        {
            _xamlMember = xamlMember;
        }

        public override void SetValue(object instance, object value, IValueContext valueContext)
        {
            PropertyAccessor.SetValue(instance, _xamlMember, value, valueContext);
        }
    }
}
