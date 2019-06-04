// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading;
using System.Linq;
using Avalonia.Data;

namespace Avalonia.Controls.Utils
{
    internal static class ValidationUtil
    {
        /// <summary>
        /// Searches a ValidationResult for the specified target member name.  If the target is null
        /// or empty, this method will return true if there are no member names at all.
        /// </summary>
        /// <param name="validationResult">ValidationResult to search.</param>
        /// <param name="target">Member name to search for.</param>
        /// <returns>True if found.</returns>
        public static bool ContainsMemberName(this ValidationResult validationResult, string target)
        {
            int memberNameCount = 0;
            foreach (string memberName in validationResult.MemberNames)
            {
                if (string.Equals(target, memberName))
                {
                    return true;
                }
                memberNameCount++;
            }
            return (memberNameCount == 0 && string.IsNullOrEmpty(target));
        }

        /// <summary>
        /// Finds an equivalent ValidationResult if one exists.
        /// </summary>
        /// <param name="collection">ValidationResults to search through.</param>
        /// <param name="target">ValidationResult to find.</param>
        /// <returns>Equal ValidationResult if found, null otherwise.</returns>
        public static ValidationResult FindEqualValidationResult(this ICollection<ValidationResult> collection, ValidationResult target)
        {
            foreach (ValidationResult oldValidationResult in collection)
            {
                if (oldValidationResult.ErrorMessage == target.ErrorMessage)
                {
                    bool movedOld = true;
                    bool movedTarget = true;
                    IEnumerator<string> oldEnumerator = oldValidationResult.MemberNames.GetEnumerator();
                    IEnumerator<string> targetEnumerator = target.MemberNames.GetEnumerator();
                    while (movedOld && movedTarget)
                    {
                        movedOld = oldEnumerator.MoveNext();
                        movedTarget = targetEnumerator.MoveNext();

                        if (!movedOld && !movedTarget)
                        {
                            return oldValidationResult;
                        }
                        if (movedOld != movedTarget || oldEnumerator.Current != targetEnumerator.Current)
                        {
                            break;
                        }
                    }
                }
            }
            return null;
        }

        public static bool IsValid(this ValidationResult result)
        {
            return result == null || result == ValidationResult.Success;
        }

        public static IEnumerable<Exception> UnpackException(Exception exception)
        {
            if (exception != null)
            {
                var aggregate = exception as AggregateException;
                var exceptions = aggregate == null ?
                    (IEnumerable<Exception>)new[] { exception } :
                    aggregate.InnerExceptions;
                var filtered = exceptions.Where(x => !(x is BindingChainException)).ToList();

                if (filtered.Count > 0)
                {
                    return filtered;
                }
            }

            return null;
        }

        /// <summary>
        /// Determines whether the collection contains an equivalent ValidationResult
        /// </summary>
        /// <param name="collection">ValidationResults to search through</param>
        /// <param name="target">ValidationResult to search for</param>
        /// <returns></returns>
        public static bool ContainsEqualValidationResult(this ICollection<ValidationResult> collection, ValidationResult target)
        {
            return (collection.FindEqualValidationResult(target) != null);
        }

        /// <summary>
        /// Adds a new ValidationResult to the collection if an equivalent does not exist.
        /// </summary>
        /// <param name="collection">ValidationResults to search through</param>
        /// <param name="value">ValidationResult to add</param>
        public static void AddIfNew(this ICollection<ValidationResult> collection, ValidationResult value)
        {
            if (!collection.ContainsEqualValidationResult(value))
            {
                collection.Add(value);
            }
        }

        private static bool ExceptionsMatch(Exception e1, Exception e2)
        {
            return e1.Message == e2.Message;
        }
        public static void AddExceptionIfNew(this ICollection<Exception> collection, Exception value)
        {
            if(!collection.Any(e => ExceptionsMatch(e, value)))
            {
                collection.Add(value);
            }
        }

        /// <summary>
        /// Performs an action and catches any non-critical exceptions.
        /// </summary>
        /// <param name="action">Action to perform</param>
        public static void CatchNonCriticalExceptions(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                if (IsCriticalException(exception))
                {
                    throw;
                }
                // Catch any non-critical exceptions
            }
        }

        /// <summary>
        /// Determines if the specified exception is un-recoverable.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns>True if the process cannot be recovered from the exception.</returns>
        public static bool IsCriticalException(Exception exception)
        {
            return (exception is OutOfMemoryException) ||
                (exception is StackOverflowException) ||
                (exception is AccessViolationException) ||
                (exception is ThreadAbortException);
        }
    }
}
