// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls;
using Perspex.Controls.Shapes;
using Perspex.Diagnostics;
using Perspex.Markup.Xaml;
using Perspex.Xaml.Interactivity;
using XamlTestApplication.Behaviors;
using XamlTestApplication.ViewModels;

namespace XamlTestApplication.Views
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
            DevTools.Attach(this);

            // TODO: Remove, only temporary for test of DragPositionBehavior
            Interaction.SetBehaviors(
                this.FindControl<Ellipse>("dragEllipse"), 
                new BehaviorCollection { new DragPositionBehavior() });
        }

        private void InitializeComponent()
        {
            PerspexXamlLoader.Load(this);
        }
    }
}