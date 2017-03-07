// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using OmniXaml;

namespace Avalonia.Markup.Xaml.Context
{
#if OMNIXAML
    public class AvaloniaLifeCycleListener : IInstanceLifeCycleListener
    {
        public void OnAfterProperties(object instance)
        {
        }

        public void OnAssociatedToParent(object instance)
        {
        }

        public void OnBegin(object instance)
        {
            var isi = instance as ISupportInitialize;
            isi?.BeginInit();
        }

        public void OnEnd(object instance)
        {
            var isi = instance as ISupportInitialize;
            isi?.EndInit();
        }
    }
#endif
}
