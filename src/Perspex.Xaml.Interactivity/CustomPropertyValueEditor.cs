// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Perspex.Xaml.Interactivity
{
    /// <summary>
    /// Enumerates possible values for reusable property value editors.
    /// </summary>
    public enum CustomPropertyValueEditor
    {
        /// <summary>
        /// Uses the storyboard picker, if supported, to edit this property at design time.
        /// </summary>
        Storyboard,
        /// <summary>
        /// Uses the state picker, if supported, to edit this property at design time.
        /// </summary>
        StateName,
        /// <summary>
        /// Uses the element-binding picker, if supported, to edit this property at design time.
        /// </summary>
        ElementBinding,
        /// <summary>
        /// Uses the property-binding picker, if supported, to edit this property at design time.
        /// </summary>
        PropertyBinding,
    }
}
