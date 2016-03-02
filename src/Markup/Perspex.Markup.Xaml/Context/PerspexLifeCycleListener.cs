// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using OmniXaml;

namespace Perspex.Markup.Xaml.Context
{
    public class PerspexLifeCycleListener : IInstanceLifeCycleListener
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
}
