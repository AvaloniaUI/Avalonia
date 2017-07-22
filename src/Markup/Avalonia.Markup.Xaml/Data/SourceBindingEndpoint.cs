// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.ComponentModel;

namespace Avalonia.Markup.Xaml.Data
{
#if OMNIXAML
    public class SourceBindingEndpoint
    {
        public Type PropertyType { get; }

        public INotifyPropertyChanged Source { get; }

        public dynamic PropertyGetter { get; }

        public Delegate PropertySetter { get; }

        public SourceBindingEndpoint(INotifyPropertyChanged source, Type propertyType, dynamic propertyGetter, Delegate propertySetter)
        {
            Source = source;
            PropertyType = propertyType;
            PropertyGetter = propertyGetter;
            PropertySetter = propertySetter;
        }
    }
#endif
}