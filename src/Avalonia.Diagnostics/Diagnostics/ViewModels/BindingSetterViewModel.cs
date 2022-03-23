﻿using System;
using Avalonia.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class BindingSetterViewModel : SetterViewModel
    {
        public BindingSetterViewModel(AvaloniaProperty property, object? value) : base(property, value)
        {
            switch (value)
            {
                case Binding binding:
                    Path = binding.Path;
                    Tint = Brushes.CornflowerBlue;

                    break;
                case CompiledBindingExtension binding:
                    Path = binding.Path.ToString();
                    Tint = Brushes.DarkGreen;

                    break;
                case TemplateBinding binding:
                    if (binding.Property is AvaloniaProperty templateProperty)
                    {
                        Path = $"{templateProperty.OwnerType.Name}.{templateProperty.Name}";
                    }
                    else
                    {
                        Path = "Unassigned";
                    }

                    Tint = Brushes.OrangeRed;

                    break;
                default:
                    throw new ArgumentException("Invalid binding type", nameof(value));
            }
        }

        public IBrush Tint { get; }

        public string Path { get; }

        public override void CopyValue()
        {
            CopyToClipboard(Path);
        }
    }
}
