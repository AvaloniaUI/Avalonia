// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Data;
using Avalonia.Markup.Xaml.Data;
using System;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    using Avalonia.Controls;
    using Avalonia.Styling;
    using Portable.Xaml;
    using Portable.Xaml.ComponentModel;
    using Portable.Xaml.Markup;
    using PortableXaml;
    using System.ComponentModel;

    [MarkupExtensionReturnType(typeof(IBinding))]
    public class BindingExtension : MarkupExtension
    {
        public BindingExtension()
        {
        }

        public BindingExtension(string path)
        {
            Path = path;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var descriptorContext = (ITypeDescriptorContext)serviceProvider;

            var pathInfo = ParsePath(Path, descriptorContext);
            ValidateState(pathInfo);

            return new Binding
            {
                Converter = Converter,
                ConverterParameter = ConverterParameter,
                ElementName = pathInfo.ElementName ?? ElementName,
                FallbackValue = FallbackValue,
                Mode = Mode,
                Path = pathInfo.Path,
                Priority = Priority,
                RelativeSource = pathInfo.RelativeSource ?? RelativeSource,
                DefaultAnchor = new WeakReference(GetDefaultAnchor((ITypeDescriptorContext)serviceProvider))
            };
        }

        private class PathInfo
        {
            public string Path { get; set; }
            public string ElementName { get; set; }
            public RelativeSource RelativeSource { get; set; }
        }

        private void ValidateState(PathInfo pathInfo)
        {
            if (pathInfo.ElementName != null && ElementName != null)
            {
                throw new InvalidOperationException(
                    "ElementName property cannot be set when an #elementName path is provided.");
            }

            if (pathInfo.RelativeSource != null && RelativeSource != null)
            {
                throw new InvalidOperationException(
                    "ElementName property cannot be set when a $self or $parent path is provided.");
            }

            if ((pathInfo.ElementName != null || ElementName != null) &&
                (pathInfo.RelativeSource != null || RelativeSource != null))
            {
                throw new InvalidOperationException(
                    "ElementName property cannot be set with a RelativeSource.");
            }
        }

        private static PathInfo ParsePath(string path, ITypeDescriptorContext context)
        {
            var result = new PathInfo();

            if (string.IsNullOrWhiteSpace(path) || path == ".")
            {
                result.Path = string.Empty;
            }
            else if (path.StartsWith("#"))
            {
                var dot = path.IndexOf('.');

                if (dot != -1)
                {
                    result.Path = path.Substring(dot + 1);
                    result.ElementName = path.Substring(1, dot - 1);
                }
                else
                {
                    result.Path = string.Empty;
                    result.ElementName = path.Substring(1);
                }
            }
            else if (path.StartsWith("$"))
            {
                var relativeSource = new RelativeSource
                {
                    Tree = TreeType.Logical
                };
                result.RelativeSource = relativeSource;
                var dot = path.IndexOf('.');
                string relativeSourceMode;
                if (dot != -1)
                {
                    result.Path = path.Substring(dot + 1);
                    relativeSourceMode = path.Substring(1, dot - 1);
                }
                else
                {
                    result.Path = string.Empty;
                    relativeSourceMode = path.Substring(1);
                }

                if (relativeSourceMode == "self")
                {
                    relativeSource.Mode = RelativeSourceMode.Self;
                }
                else if (relativeSourceMode == "parent")
                {
                    relativeSource.Mode = RelativeSourceMode.FindAncestor;
                    relativeSource.AncestorLevel = 1;
                }
                else if (relativeSourceMode.StartsWith("parent["))
                {
                    relativeSource.Mode = RelativeSourceMode.FindAncestor;
                    var parentConfigStart = relativeSourceMode.IndexOf('[');
                    if (!relativeSourceMode.EndsWith("]"))
                    {
                        throw new InvalidOperationException("Invalid RelativeSource binding syntax. Expected matching ']' for '['.");
                    }
                    var parentConfigParams = relativeSourceMode.Substring(parentConfigStart + 1).TrimEnd(']').Split(';');
                    if (parentConfigParams.Length > 2 || parentConfigParams.Length == 0)
                    {
                        throw new InvalidOperationException("Expected either 1 or 2 parameters for RelativeSource binding syntax");
                    }
                    else if (parentConfigParams.Length == 1)
                    {
                        if (int.TryParse(parentConfigParams[0], out int level))
                        {
                            relativeSource.AncestorType = null;
                            relativeSource.AncestorLevel = level + 1;
                        }
                        else
                        {
                            relativeSource.AncestorType = LookupAncestorType(parentConfigParams[0], context);
                        }
                    }
                    else
                    {
                        relativeSource.AncestorType = LookupAncestorType(parentConfigParams[0], context);
                        relativeSource.AncestorLevel = int.Parse(parentConfigParams[1]) + 1;
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Invalid RelativeSource binding syntax: {relativeSourceMode}");
                }
            }
            else
            {
                result.Path = path;
            }

            return result;
        }

        private static Type LookupAncestorType(string ancestorTypeName, ITypeDescriptorContext context)
        {
            var parts = ancestorTypeName.Split(':');
            if (parts.Length == 0 || parts.Length > 2)
            {
                throw new InvalidOperationException("Invalid type name");
            }

            if (parts.Length == 1)
            {
                return context.ResolveType(string.Empty, parts[0]);
            }
            else
            {
                return context.ResolveType(parts[0], parts[1]);
            }
        }

        private static object GetDefaultAnchor(ITypeDescriptorContext context)
        {
            object anchor = null;

            // The target is not a control, so we need to find an anchor that will let us look
            // up named controls and style resources. First look for the closest IControl in
            // the context.
            anchor = context.GetFirstAmbientValue<IControl>();

            // If a control was not found, then try to find the highest-level style as the XAML
            // file could be a XAML file containing only styles.
            return anchor ??
                    context.GetService<IRootObjectProvider>()?.RootObject as IStyle ??
                    context.GetLastOrDefaultAmbientValue<IStyle>();
        }

        public IValueConverter Converter { get; set; }

        public object ConverterParameter { get; set; }

        public string ElementName { get; set; }

        public object FallbackValue { get; set; } = AvaloniaProperty.UnsetValue;

        public BindingMode Mode { get; set; }

        [ConstructorArgument("path")]
        public string Path { get; set; }

        public BindingPriority Priority { get; set; } = BindingPriority.LocalValue;

        public object Source { get; set; }

        public RelativeSource RelativeSource { get; set; }
    }
}