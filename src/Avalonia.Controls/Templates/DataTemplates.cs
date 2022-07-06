using System;
using Avalonia.Collections;

namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// A collection of <see cref="IDataTemplate"/>s.
    /// </summary>
    public class DataTemplates : AvaloniaList<IDataTemplate>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataTemplates"/> class.
        /// </summary>
        public DataTemplates()
        {
            ResetBehavior = ResetBehavior.Remove;
            
            Validate += ValidateDataTemplate;
        }

        private static void ValidateDataTemplate(IDataTemplate template)
        {
            var valid = template switch
            {
                ITypedDataTemplate typed => typed.DataType is not null,
                _ => true
            };
            
            if (!valid)
            {
                throw new InvalidOperationException("DataTemplate inside of DataTemplates must have a DataType set. Set DataType property or use ItemTemplate with single template instead.");
            }
        }
    }
}