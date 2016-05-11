// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using ReactiveUI;

namespace XamlTestApplication.ViewModels
{
    public class TestNode : ReactiveObject
    {
        private bool _isExpanded;

        public string Header { get; set; }
        public string SubHeader { get; set; }
        public IEnumerable<TestNode> Children { get; set; }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { this.RaiseAndSetIfChanged(ref this._isExpanded, value); }
        }
    }
}