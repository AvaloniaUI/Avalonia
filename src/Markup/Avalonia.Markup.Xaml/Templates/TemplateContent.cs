// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls;
using System.Collections.Generic;

namespace Avalonia.Markup.Xaml.Templates
{
    
    public static class TemplateContent
    {
        public static IControl Load(object templateContent)
        {
            if (templateContent is Func<IServiceProvider, object> direct)
            {
                return (IControl)direct(null);
            }
            throw new ArgumentException(nameof(templateContent));
        }
    }
}
