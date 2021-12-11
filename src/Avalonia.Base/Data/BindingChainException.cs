﻿using System;

#nullable enable

namespace Avalonia.Data
{
    /// <summary>
    /// An exception returned through <see cref="BindingNotification"/> signaling that a
    /// requested binding expression could not be evaluated because of a null in one of the links
    /// of the binding chain.
    /// </summary>
    public class BindingChainException : Exception
    {
        private string _message;

        /// <summary>
        /// Initializes a new instance of the <see cref="BindingChainException"/> class.
        /// </summary>
        public BindingChainException()
        {
            _message = "Binding error";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BindingChainException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public BindingChainException(string message)
        {
            _message = message;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BindingChainException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="expression">The expression.</param>
        /// <param name="errorPoint">
        /// The point in the expression at which the error was encountered.
        /// </param>
        public BindingChainException(string message, string expression, string errorPoint)
        {
            _message = message;
            Expression = expression;
            ExpressionErrorPoint = errorPoint;
        }

        /// <summary>
        /// Gets the expression that could not be evaluated.
        /// </summary>
        public string? Expression { get; protected set; }

        /// <summary>
        /// Gets the point in the expression at which the error occurred.
        /// </summary>
        public string? ExpressionErrorPoint { get; protected set; }

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                if (Expression != null && ExpressionErrorPoint != null)
                {
                    return $"{_message} in expression '{Expression}' at '{ExpressionErrorPoint}'.";
                }
                else if (ExpressionErrorPoint != null)
                {
                    return $"{_message} in expression '{ExpressionErrorPoint}'.";
                }
                else
                {
                    return $"{_message} in expression.";
                }
            }
        }
    }
}
