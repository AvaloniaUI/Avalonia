// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Windows.Threading;
using Perspex;
using Perspex.Collections;
using Perspex.Controls;
using Perspex.Controls.Templates;
using ReactiveUI;
using XamlTestApplication.Views;

namespace XamlTestApplication
{
    internal class Item
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    internal class Node
    {
        public Node()
        {
            Children = new PerspexList<Node>();
        }

        public string Name { get; set; }
        public PerspexList<Node> Children { get; set; }
    }

    internal class Program
    {
        private static void Main()
        {
            var foo = Dispatcher.CurrentDispatcher;

            App application = new App
            {
                DataTemplates = new DataTemplates
                {
                    new FuncTreeDataTemplate<Node>(
                        x => new TextBlock { Text = x.Name },
                        x => x.Children,
                        x => true),
                },
            };

            var testCommand = ReactiveCommand.Create();
            testCommand.Subscribe(_ => Debug.WriteLine("Test command executed."));

            var window = new MainWindow();
            window.Show();
            Application.Current.Run(window);
        }
    }
}
