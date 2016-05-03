using Perspex.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perspex.Markup.Data
{
    public interface IFilterableValidationStatus : IValidationStatus
    {
        /// <summary>
        /// Checks if this validation status came from a currently enabled method of validation checking.
        /// </summary>
        /// <param name="enabledMethods">The enabled methods of validation checking.</param>
        /// <returns>True if enabled; otherwise, false.</returns>
        bool Match(ValidationMethods enabledMethods);
    }
}
