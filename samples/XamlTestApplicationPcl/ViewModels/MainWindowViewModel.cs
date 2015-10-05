// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;

namespace XamlTestApplication.ViewModels
{
    public class MainWindowViewModel
    {
        public MainWindowViewModel()
        {
            Items = new List<TestItem>();

            for (int i = 0; i < 10; ++i)
            {
                Items.Add(new TestItem($"Item {i}", $"Item {i} Value"));
            }

            Nodes = new List<TestNode>
            {
                new TestNode
                {
                    Header = "Root",
                    SubHeader = "Root Item",
                    Children = new[]
                    {
                        new TestNode
                        {
                            Header = "Child 1",
                            SubHeader = "Child 1 Value",
                        },
                        new TestNode
                        {
                            Header = "Child 2",
                            SubHeader = "Child 2 Value",
                            Children = new[]
                            {
                                new TestNode
                                {
                                    Header = "Grandchild",
                                    SubHeader = "Grandchild Value",
                                },
                                new TestNode
                                {
                                    Header = "Grandmaster Flash",
                                    SubHeader = "White Lines",
                                },
                            }
                        },
                    }
                }
            };
        }

        public List<TestItem> Items { get; }
        public List<TestNode> Nodes { get; }
    }
}
