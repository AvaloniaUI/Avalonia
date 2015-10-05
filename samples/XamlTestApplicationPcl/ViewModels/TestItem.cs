// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace XamlTestApplication.ViewModels
{
    public class TestItem
    {
        public TestItem(string header, string subheader)
        {
            Header = header;
            SubHeader = subheader;
        }

        public string Header { get; }
        public string SubHeader { get; }
    }
}
