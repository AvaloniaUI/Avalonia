// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls;
using Perspex.Controls.Shapes;
using Perspex.Diagnostics;
using Perspex.Markup.Xaml;
using Perspex.Xaml.Interactions.Core;
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

            XamlBehaviorsTest();
        }

        private void XamlBehaviorsTest()
        {
            //
            // TODO: Remove code below used only for temporary test of DragPositionBehavior
            //

            /*
            <Interactivity:Interaction.Behaviors>
                <behaviors:DragPositionBehavior/>
            </Interactivity:Interaction.Behaviors>
            */

            Interaction.SetBehaviors(
                this.FindControl<Ellipse>("dragEllipse"),
                new BehaviorCollection { new DragPositionBehavior() });

            //
            // TODO: Remove code below used only for temporary test of EventTriggerBehavior
            //

            /*
            <Interactivity:Interaction.Behaviors>
                <Interactions:EventTriggerBehavior EventName="Click" SourceObject="{Binding ElementName=button}">
                    <Interactions:ChangePropertyAction TargetObject="{Binding ElementName=DataTriggerText}" PropertyName="Text" Value="Hello!"/>
                </Interactions:EventTriggerBehavior>
            </Interactivity:Interaction.Behaviors>
            */

            var button = this.FindControl<Button>("button");

            var eventTriggerBehavior = new EventTriggerBehavior()
            {
                EventName = "Click",
                SourceObject = button
            };

            eventTriggerBehavior.SetValue(
                EventTriggerBehavior.ActionsProperty,
                new ActionCollection
                {
                    new ChangePropertyAction()
                    {
                        TargetObject = this.FindControl<TextBox>("DataTriggerText"),
                        PropertyName = "Text",
                        Value = "Hello!"
                    }
                });

            Interaction.SetBehaviors(
                button,
                new BehaviorCollection { eventTriggerBehavior });
        }

        private void InitializeComponent()
        {
            PerspexXamlLoader.Load(this);
        }
    }
}