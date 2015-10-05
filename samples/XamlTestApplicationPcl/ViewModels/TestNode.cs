// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;

namespace XamlTestApplication.ViewModels
{
    public class TestNode
    {
        public string Header { get; set; }
        public string SubHeader { get; set; }
        public IEnumerable<TestNode> Children { get; set; }
    }
}