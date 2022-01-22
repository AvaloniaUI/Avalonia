using System;

namespace Avalonia.Data
{
    /// <summary>
    /// Exception, which wrap validation errors.
    /// </summary>
    public class DataValidationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataValidationException"/> class.
        /// </summary>
        /// <param name="errorData">Data of validation error.</param>
        public DataValidationException(object? errorData) : base(errorData?.ToString())
        {
            ErrorData = errorData;
        }

        public object? ErrorData { get; }
    }
}
