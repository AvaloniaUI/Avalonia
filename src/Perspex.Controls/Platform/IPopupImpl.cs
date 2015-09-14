// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Controls;
using Perspex.Input.Raw;

namespace Perspex.Platform
{
    public interface IPopupImpl : ITopLevelImpl
    {
        void SetPosition(Point p);

        void Show();

        void Hide();
    }
}
