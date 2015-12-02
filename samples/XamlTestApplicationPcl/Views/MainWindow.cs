// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls;
using Perspex.Diagnostics;
using Perspex.Markup.Xaml;
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
            var dragEllipse = this.FindControl<Perspex.Controls.Shapes.Ellipse>("dragEllipse");
            if (dragEllipse != null)
            {
                Perspex.Xaml.Interactivity.Interaction.SetBehaviors(dragEllipse, new Perspex.Xaml.Interactivity.BehaviorCollection()
                {
                    new Behaviors.DragPositionBehavior()
                });
            }
        }

        private void InitializeComponent()
        {
            PerspexXamlLoader.Load(this);
        }
    }
}