// -----------------------------------------------------------------------
// <copyright file="DevTools.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics
{
    using Perspex.Controls;
    using System.Reactive.Linq;

    public class DevTools : Decorator
    {
        public static readonly PerspexProperty<Control> RootProperty =
            PerspexProperty.Register<DevTools, Control>("Root");

        public DevTools()
        {
            this.Content = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions
                {
                    new ColumnDefinition(1, GridUnitType.Star),
                    new ColumnDefinition(3, GridUnitType.Star),
                },
                Children = new Controls
                {
                    new TreeView
                    {
                        DataTemplates = new DataTemplates
                        {
                            new TreeDataTemplate<IVisual>(GetHeader, x => x.VisualChildren),
                        },
                        [!TreeView.ItemsProperty] = this[!DevTools.RootProperty].Select(x => new[] { x }),
                    }
                }
            };
        }

        public Control Root
        {
            get { return this.GetValue(RootProperty); }
            set { this.SetValue(RootProperty, value); }
        }

        private static Control GetHeader(IVisual visual)
        {
            Control control = visual as Control;
            TextBlock result = new TextBlock();
            result.Text = visual.GetType().Name;

            if (control != null && control.TemplatedParent != null)
            {
                result.FontStyle = Media.FontStyle.Italic;
            }

            return result;
        }
    }
}
