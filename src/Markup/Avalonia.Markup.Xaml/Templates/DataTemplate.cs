using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace Avalonia.Markup.Xaml.Templates
{
    public class DataTemplate : IRecyclingDataTemplate, ITypedDataTemplate, IVirtualizingDataTemplate
    {
        [DataType]
        public Type? DataType { get; set; }

        [Content]
        [TemplateContent]
        public object? Content { get; set; }

        /// <summary>
        /// Gets or sets whether this template supports content virtualization.
        /// When true, content controls are recycled based on DataType.
        /// Default is false for backward compatibility.
        /// </summary>
        public bool EnableVirtualization { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum pool size per key for content virtualization.
        /// Default is 5.
        /// </summary>
        public int MaxPoolSizePerKey { get; set; } = 5;

        /// <summary>
        /// Gets the minimum number of controls to keep in the recycle pool
        /// for each key. Default is 2.
        /// This is only used when warmup is enabled
        /// </summary>
        public int MinPoolSizePerKey { get; } = 2;

        public bool Match(object? data)
        {
            if (DataType == null)
            {
                return true;
            }
            else
            {
                return DataType.IsInstanceOfType(data);
            }
        }

        public Control? Build(object? data) => Build(data, null);

        public Control? Build(object? data, Control? existing)
        {
            // If virtualizing and recycled control provided, use it
            if (EnableVirtualization && existing != null)
                return existing;

            // Otherwise create new from template
            return existing ?? TemplateContent.Load(Content)?.Result;
        }

        /// <summary>
        /// Gets a key that identifies which recycling pool this data belongs to.
        /// </summary>
        public object? GetKey(object? data)
        {
            if (!EnableVirtualization)
                return null;

            // Use DataType as the key (all objects of same type share same pool)
            return DataType ?? data?.GetType();
        }
    }
}
