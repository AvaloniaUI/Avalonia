// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Controls.Presenters
{
    public interface IItemsPresenter : IPresenter
    {
        IPanel Panel { get; }

        void ScrollIntoView(object item);
    }
}
