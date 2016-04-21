using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perspex.Data
{
    /// <summary>
    /// An immutable struct that contains validation information for a <see cref="PerspexObject"/> that validates a single property.
    /// </summary>
    public struct ObjectValidationStatus : IValidationStatus
    {
        private Dictionary<Type, IValidationStatus> currentValidationStatus;

        public bool IsValid => currentValidationStatus?.Values.All(status => status.IsValid) ?? true;

        /// <summary>
        /// Constructs the structure with the given validation information.
        /// </summary>
        /// <param name="validations">The validation information</param>
        public ObjectValidationStatus(Dictionary<Type, IValidationStatus> validations)
            :this()
        {
            currentValidationStatus = validations;
        }

        /// <summary>
        /// Creates a new status with the updated information.
        /// </summary>
        /// <param name="status">The updated status information.</param>
        /// <returns>The new validation status.</returns>
        public ObjectValidationStatus UpdateValidationStatus(IValidationStatus status)
        {
            var newStatus = new Dictionary<Type, IValidationStatus>(currentValidationStatus ??
                                                                new Dictionary<Type, IValidationStatus>());
            newStatus[status.GetType()] = status;
            return new ObjectValidationStatus(newStatus);
        }

        public IEnumerable<IValidationStatus> StatusInformation => currentValidationStatus.Values;
    }
}
