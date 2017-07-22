// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reflection;
using OmniXaml;
using OmniXaml.Typing;
using Avalonia.Controls;

namespace Avalonia.Markup.Xaml.Context
{
#if OMNIXAML
    public class AvaloniaXamlType : XamlType
    {
        public AvaloniaXamlType(Type type,
            ITypeRepository typeRepository,
            ITypeFactory typeFactory,
            ITypeFeatureProvider featureProvider) : base(type, typeRepository, typeFactory, featureProvider)
        {
        }

        public override OmniXaml.INameScope GetNamescope(object instance)
        {
            var result = instance as OmniXaml.INameScope;

            if (result == null)
            {
                var control = instance as Control;

                if (control != null)
                {
                    var avaloniaNs = (instance as Avalonia.Controls.INameScope) ?? NameScope.GetNameScope(control);

                    if (avaloniaNs != null)
                    {
                        result = new NameScopeWrapper(avaloniaNs);
                    }
                }
            }

            return result;
        }

        protected override Member LookupMember(string name)
        {
            return new AvaloniaXamlMember(name, this, TypeRepository, FeatureProvider);
        }

        protected override AttachableMember LookupAttachableMember(string name)
        {
            // OmniXAML seems to require a getter and setter even though we don't use them.
            var getter = UnderlyingType.GetTypeInfo().GetDeclaredMethod("Get" + name);
            var setter = UnderlyingType.GetTypeInfo().GetDeclaredMethod("Set" + name);
            return new AvaloniaAttachableXamlMember(name, this, getter, setter, TypeRepository, FeatureProvider);
        }

        public override string ToString()
        {
            return "Avalonia XAML Type " + base.ToString();
        }
    }
#endif
}