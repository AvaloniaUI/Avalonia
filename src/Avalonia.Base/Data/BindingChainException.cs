// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Data
{
    /// <summary>
    /// An exception returned through <see cref="BindingNotification"/> signalling that a
    /// requested binding expression could not be evaluated because of a null in one of the links
    /// of the binding chain.
    /// </summary>
    public class BindingChainException : Exception
    {
        private string _message;

        /// <summary>
        /// Initalizes a new instance of the <see cref="BindingChainException"/> class.
        /// </summary>
        public BindingChainException()
        {
        }

        /// <summary>
        /// Initalizes a new instance of the <see cref="BindingChainException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public BindingChainException(string message)
        {
            _message = message;
        }

        /// <summary>
        /// Initalizes a new instance of the <see cref="BindingChainException"/> class.
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
        public string Expression { get; protected set; }

        /// <summary>
        /// Gets the point in the expression at which the error occured.
        /// </summary>
        public string ExpressionErrorPoint { get; protected set; }

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
