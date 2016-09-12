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
    public class BindingChainNullException : Exception
    {
        private string _message;

        /// <summary>
        /// Initalizes a new instance of the <see cref="BindingChainNullException"/> class.
        /// </summary>
        public BindingChainNullException()
        {
        }

        /// <summary>
        /// Initalizes a new instance of the <see cref="BindingChainNullException"/> class.
        /// </summary>
        public BindingChainNullException(string message)
        {
            _message = message;
        }

        /// <summary>
        /// Initalizes a new instance of the <see cref="BindingChainNullException"/> class.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="expressionNullPoint">
        /// The point in the expression at which the null was encountered.
        /// </param>
        public BindingChainNullException(string expression, string expressionNullPoint)
        {
            Expression = expression;
            ExpressionNullPoint = expressionNullPoint;
        }

        /// <summary>
        /// Gets the expression that could not be evaluated.
        /// </summary>
        public string Expression { get; protected set; }

        /// <summary>
        /// Gets the point in the expression at which the null was encountered.
        /// </summary>
        public string ExpressionNullPoint { get; protected set; }

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                if (_message == null)
                {
                    _message = BuildMessage();
                }

                return _message;
            }
        }

        private string BuildMessage()
        {
            if (Expression != null && ExpressionNullPoint != null)
            {
                return $"'{ExpressionNullPoint}' is null in expression '{Expression}'.";
            }
            else if (ExpressionNullPoint != null)
            {
                return $"'{ExpressionNullPoint}' is null in expression.";
            }
            else
            {
                return "Null encountered in binding expression.";
            }
        }
    }
}
