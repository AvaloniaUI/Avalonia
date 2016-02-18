// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using OmniXaml;
using Perspex.LogicalTree;
using Perspex.Styling;

namespace Perspex.Markup.Xaml.MarkupExtensions
{
    public class StyleResourceExtension : MarkupExtension
    {
        public StyleResourceExtension(string name)
        {
            Name = name;
        }

        public override object ProvideValue(MarkupExtensionContext extensionContext)
        {
            var styleHost = extensionContext.TargetObject as IStyleHost;

            if (styleHost == null)
            {
                throw new ParseException(
                    $"StyleResource cannot be assigned to an object of type '{extensionContext.TargetObject.GetType()}'.");
            }

            // HACK: This should be as simple as:
            //     return styleHost.FindStyleResource(Name);
            // Waiting on OmniXAML issue #84 to be fixed before it can be that simple though.
            var po = (PerspexObject)styleHost;
            var parent = PerspexPropertyRegistry.Instance.FindRegistered(po, "Parent");

            po.GetObservable(parent)
                .Where(x => x != PerspexProperty.UnsetValue && x != null)
                .Take(1)
                .Subscribe(_ =>
                {
                    var resource = styleHost.FindStyleResource(Name);

                    if (resource != PerspexProperty.UnsetValue)
                    {
                        extensionContext.TargetProperty.SetValue(extensionContext.TargetObject, resource);
                    }
                });

            return PerspexProperty.UnsetValue;
        }

        public string Name { get; set; }
    }
}