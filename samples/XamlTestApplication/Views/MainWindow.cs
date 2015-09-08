// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using OmniXaml;
using Perspex.Controls;
using Perspex.Diagnostics;
using Perspex.Markup.Xaml;

namespace XamlTestApplication.Views
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            DevTools.Attach(this);
        }

        private void InitializeComponent()
        {
            PerspexXamlLoader.Load(this);
        }
    }
}