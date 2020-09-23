using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;

namespace Avalonia.Diagnostics.Models
{
    class FavoriteProperties
    {
        public string Name { get; set; }
        [Newtonsoft.Json.JsonProperty]
        public string[] Properties { get; internal set; }

        public override string ToString()
        {
            return Name;
        }

        public static readonly FavoriteProperties[] Default =
            new[]
            {
                new FavoriteProperties(){ Name = "All"},
                new FavoriteProperties(){ Name = "Layout", Properties  = new[]
                    {
                       nameof(Layoutable.Bounds),
                       nameof(Layoutable.Clip),
                       nameof(Layoutable.DesiredSize),
                       nameof(Layoutable.Height),
                       nameof(Layoutable.HorizontalAlignment),
                       nameof(Layoutable.IsVisible),
                       nameof(Layoutable.Margin),
                       nameof(Layoutable.MaxHeight),
                       nameof(Layoutable.MaxWidth),
                       nameof(Layoutable.MinHeight),
                       nameof(Layoutable.MinWidth),
                       nameof(TemplatedControl.Padding),
                       nameof(Layoutable.RenderTransform),
                       nameof(Layoutable.RenderTransformOrigin),
                       nameof(Layoutable.TransformedBounds),
                       nameof(Layoutable.UseLayoutRounding),
                       nameof(Layoutable.VerticalAlignment),
                       nameof(Layoutable.Width),
                       nameof(Layoutable.ZIndex),
                    }
                },
                new FavoriteProperties(){Name = "Font and Color", Properties =new[]
                {
                    nameof(TemplatedControl.Background),
                    nameof(TemplatedControl.BorderBrush),
                    nameof(TemplatedControl.BorderThickness),
                    nameof(TemplatedControl.FontFamily),
                    nameof(TemplatedControl.FontSize),
                    nameof(TemplatedControl.FontStyle),
                    nameof(TemplatedControl.FontWeight),
                    nameof(TemplatedControl.Foreground),
                } },
                new FavoriteProperties(){Name = "Input", Properties =new[]
                {
                    nameof(InputElement.Focusable),
                    nameof(InputElement.GestureRecognizers),
                    nameof(InputElement.IsEnabled),
                    nameof(InputElement.IsFocused),
                    nameof(InputElement.IsHitTestVisible),
                    nameof(InputElement.IsPointerOver),
                    nameof(InputElement.IsVisible),
                    nameof(InputElement.KeyBindings),
                } },
                new FavoriteProperties(){Name = "Style and Tamplete", Properties =new[]
                {
                    nameof(Layoutable.Classes),
                    nameof(TemplatedControl.Template),
                    nameof(TemplatedControl.DataTemplates),
                    nameof(TemplatedControl.Styles),
                    nameof(TemplatedControl.Resources),
                } },
                new FavoriteProperties(){Name = "ItemsControl", Properties =new[]
                {
                    nameof(ItemsControl.Items),
                    nameof(ItemsControl.ItemCount),
                    nameof(ItemsControl.ItemsPanel),
                    nameof(ItemsControl.ItemTemplate),                    
                } },
                new FavoriteProperties(){Name = "SelectingItemsControl", Properties =new[]
                {
                    nameof(SelectingItemsControl.Items),
                    nameof(SelectingItemsControl.ItemCount),
                    nameof(SelectingItemsControl.ItemsPanel),
                    nameof(SelectingItemsControl.ItemTemplate),
                    nameof(SelectingItemsControl.SelectedIndex),
                    nameof(SelectingItemsControl.SelectedItem),
                    nameof(SelectingItemsControl.AutoScrollToSelectedItem),
                    "SelectedItems",
                    "SelectionMode",
                } },
            };
    }
}
