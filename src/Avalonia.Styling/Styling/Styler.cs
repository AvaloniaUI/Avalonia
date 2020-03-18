// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

#nullable enable

namespace Avalonia.Styling
{
    public class Styler : IStyler
    {
        public void ApplyStyles(IStyleable target)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));

            if (target is IStyleHost styleHost)
            {
                ApplyStyles(target, styleHost);
            }
        }

        private void ApplyStyles(IStyleable target, IStyleHost host)
        {
            var parent = host.StylingParent;

            if (parent != null)
            {
                ApplyStyles(target, parent);
            }

            if (host.IsStylesInitialized)
            {
                host.Styles.TryAttach(target, host);
            }
        }
    }
}
