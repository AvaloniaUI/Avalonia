// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// Selects a member of an object using a <see cref="Func{TObject, TMember}"/>.
    /// </summary>
    public class FuncMemberSelector<TObject, TMember> : IMemberSelector
    {
        private readonly Func<TObject, TMember> _selector;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncMemberSelector{TObject, TMember}"/>
        /// class.
        /// </summary>
        /// <param name="selector">The selector.</param>
        public FuncMemberSelector(Func<TObject, TMember> selector)
        {
            this._selector = selector;
        }

        /// <summary>
        /// Selects a member of an object.
        /// </summary>
        /// <param name="o">The obeject.</param>
        /// <returns>The selected member.</returns>
        public object Select(object o)
        {
            return (o is TObject) ? _selector((TObject)o) : default(TMember);
        }
    }
}