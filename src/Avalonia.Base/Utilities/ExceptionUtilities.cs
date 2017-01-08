using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Utilities
{
    internal static class ExceptionUtilities
    {
        public static string GetMessage(Exception e)
        {
            var aggregate = e as AggregateException;

            if (aggregate != null)
            {
                return string.Join(" | ", aggregate.InnerExceptions.Select(x => x.Message));
            }

            return e.Message;
        }
    }
}
